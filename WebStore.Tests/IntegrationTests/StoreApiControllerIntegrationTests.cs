using System.Net;
using System.Net.Http.Json;
using WebStore.ViewModels.VM;

namespace WebStore.Tests.IntegrationTests
{
    public class StoreApiControllerIntegrationTests : BaseApiIntegrationTests
    {
        private const string Url = "/api/StoreApi";
        private const int EmployeeId = 3;

        public StoreApiControllerIntegrationTests(WebStoreApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAllStoresTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync(Url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var stores = await response.Content.ReadFromJsonAsync<List<StoreVm>>();
            Assert.Equal(QueryDatabase(db => db.StationaryStores.Count()), stores!.Count);
            Assert.Contains(stores, s => s.Name == "WebStore Gliwice");
        }

        [Fact]
        public async Task GetStoreTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var store = await response.Content.ReadFromJsonAsync<StoreVm>();
            Assert.Equal(1, store!.Id);
            Assert.Equal("WebStore Gliwice", store.Name);
            Assert.Contains(store.Addresses, a => a.Id == 3);
        }

        [Fact]
        public async Task GetNotExistingStoreReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/{NotExistingId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddStoreTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync(Url, new AddOrUpdateStoreVm { Name = "WebStore Zabrze" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var store = await response.Content.ReadFromJsonAsync<StoreVm>();
            Assert.True(store!.Id > 0);
            Assert.Equal("WebStore Zabrze", store.Name);
            Assert.True(QueryDatabase(db => db.StationaryStores.Any(s => s.Id == store.Id)));
        }

        [Fact]
        public async Task AddStoreWithZeroIdTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var storesCountBefore = QueryDatabase(db => db.StationaryStores.Count());

            var response = await client.PostAsJsonAsync(Url, new AddOrUpdateStoreVm { Id = 0, Name = "WebStore Rybnik" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var store = await response.Content.ReadFromJsonAsync<StoreVm>();
            Assert.True(store!.Id > 0);
            Assert.Equal(storesCountBefore + 1, QueryDatabase(db => db.StationaryStores.Count()));
        }

        [Fact]
        public async Task AddStoreWithExistingIdReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdStore = await CreateStoreAsync(client, "WebStore Tychy");
            var storesCountBefore = QueryDatabase(db => db.StationaryStores.Count());

            var response = await client.PostAsJsonAsync(Url, new AddOrUpdateStoreVm { Id = createdStore.Id, Name = "WebStore Tychy Nadpisany" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(storesCountBefore, QueryDatabase(db => db.StationaryStores.Count()));
            Assert.Equal("WebStore Tychy", QueryDatabase(db => db.StationaryStores.Single(s => s.Id == createdStore.Id).Name));
        }

        [Fact]
        public async Task AddStoreWithNotExistingIdReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var storesCountBefore = QueryDatabase(db => db.StationaryStores.Count());

            var response = await client.PostAsJsonAsync(Url, new AddOrUpdateStoreVm { Id = NotExistingId, Name = "WebStore Obce Id" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(storesCountBefore, QueryDatabase(db => db.StationaryStores.Count()));
            Assert.False(QueryDatabase(db => db.StationaryStores.Any(s => s.Name == "WebStore Obce Id")));
        }

        [Fact]
        public async Task UpdateStoreTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdStore = await CreateStoreAsync(client, "WebStore Bytom");

            var response = await client.PutAsJsonAsync(Url, new AddOrUpdateStoreVm { Id = createdStore.Id, Name = "WebStore Bytom Centrum" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var store = await response.Content.ReadFromJsonAsync<StoreVm>();
            Assert.Equal(createdStore.Id, store!.Id);
            Assert.Equal("WebStore Bytom Centrum", store.Name);
            Assert.Equal("WebStore Bytom Centrum", QueryDatabase(db => db.StationaryStores.Single(s => s.Id == createdStore.Id).Name));
        }

        [Fact]
        public async Task UpdateNotExistingStoreReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var storesCountBefore = QueryDatabase(db => db.StationaryStores.Count());

            var response = await client.PutAsJsonAsync(Url, new AddOrUpdateStoreVm { Id = NotExistingId, Name = "Brak" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(storesCountBefore, QueryDatabase(db => db.StationaryStores.Count()));
            Assert.False(QueryDatabase(db => db.StationaryStores.Any(s => s.Name == "Brak")));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        public async Task UpdateStoreWithoutIdReturnsBadRequestTest(int? id)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var storesCountBefore = QueryDatabase(db => db.StationaryStores.Count());

            var response = await client.PutAsJsonAsync(Url, new AddOrUpdateStoreVm { Id = id, Name = "WebStore Bez Id" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(storesCountBefore, QueryDatabase(db => db.StationaryStores.Count()));
            Assert.False(QueryDatabase(db => db.StationaryStores.Any(s => s.Name == "WebStore Bez Id")));
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task AddOrUpdateStoreWithInvalidModelReturnsBadRequestTest(string method)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            using var request = new HttpRequestMessage(new HttpMethod(method), Url)
            {
                Content = JsonContent.Create(new { id = 1 })
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Name", body);
            Assert.Contains("Nazwa sklepu jest wymagana.", body);
        }

        [Fact]
        public async Task DeleteStoreTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdStore = await CreateStoreAsync(client, "WebStore do usunięcia");

            var response = await client.DeleteAsync($"{Url}/{createdStore.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(QueryDatabase(db => db.StationaryStores.Any(s => s.Id == createdStore.Id)));
        }

        [Fact]
        public async Task DeleteNotExistingStoreReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.DeleteAsync($"{Url}/{NotExistingId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteStoreWithEmployeesReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdStore = await CreateStoreAsync(client, "WebStore z pracownikiem");
            var assignResponse = await client.PostAsync($"{Url}/{createdStore.Id}/Employees/{EmployeeId}", null);
            assignResponse.EnsureSuccessStatusCode();

            var response = await client.DeleteAsync($"{Url}/{createdStore.Id}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.True(QueryDatabase(db => db.StationaryStores.Any(s => s.Id == createdStore.Id)));
        }

        [Fact]
        public async Task AddEmployeeToStoreTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdStore = await CreateStoreAsync(client, "WebStore Rybnik");

            var response = await client.PostAsync($"{Url}/{createdStore.Id}/Employees/{EmployeeId}", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var store = await response.Content.ReadFromJsonAsync<StoreVm>();
            Assert.Equal(createdStore.Id, store!.Id);
            Assert.Contains(EmployeeId, store.EmployeeIds);
            Assert.Equal(createdStore.Id, QueryDatabase(db => db.StationaryStoreEmployees.Single(e => e.Id == EmployeeId).StationaryStoreId));
        }

        [Fact]
        public async Task AddEmployeeToNotExistingStoreReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsync($"{Url}/{NotExistingId}/Employees/{EmployeeId}", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddNotExistingEmployeeToStoreReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsync($"{Url}/1/Employees/{NotExistingId}", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddAddressToStoreTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdStore = await CreateStoreAsync(client, "WebStore Tychy");

            var response = await client.PostAsJsonAsync($"{Url}/{createdStore.Id}/Addresses", CreateAddressVm());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var store = await response.Content.ReadFromJsonAsync<StoreVm>();
            var address = Assert.Single(store!.Addresses);
            Assert.Equal("Piłsudskiego", address.Street);
            Assert.Equal(createdStore.Id, address.StationaryStoreId);
            Assert.True(QueryDatabase(db => db.Addresses.Any(a => a.StationaryStoreId == createdStore.Id && a.Street == "Piłsudskiego")));
        }

        [Fact]
        public async Task AddAddressToNotExistingStoreReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync($"{Url}/{NotExistingId}/Addresses", CreateAddressVm());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddInvalidAddressToStoreReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync($"{Url}/1/Addresses", new { street = "Bez miasta" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("City", body);
            Assert.Contains("Miasto jest wymagane.", body);
        }

        [Fact]
        public async Task AddAddressWithNotExistingCustomerToStoreReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdStore = await CreateStoreAsync(client, "WebStore Bytom");
            var addressVm = CreateAddressVm();
            addressVm.CustomerId = NotExistingId;

            var response = await client.PostAsJsonAsync($"{Url}/{createdStore.Id}/Addresses", addressVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains($"Nie znaleziono klienta o Id {NotExistingId}.", await response.Content.ReadAsStringAsync());
            Assert.False(QueryDatabase(db => db.Addresses.Any(a => a.StationaryStoreId == createdStore.Id)));
        }

        [Theory]
        [InlineData("GET", Url)]
        [InlineData("GET", Url + "/1")]
        [InlineData("POST", Url)]
        [InlineData("PUT", Url)]
        [InlineData("DELETE", Url + "/1")]
        [InlineData("POST", Url + "/1/Employees/3")]
        [InlineData("POST", Url + "/1/Addresses")]
        public async Task RequestWithoutTokenReturnsUnauthorizedTest(string method, string url)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await SendAnonymousRequestAsync(method, url));
        }

        private static AddOrUpdateAddressVm CreateAddressVm()
        {
            return new AddOrUpdateAddressVm
            {
                Street = "Piłsudskiego",
                BuildingNumber = "10",
                PostalCode = "43-100",
                City = "Tychy",
                Country = "Polska"
            };
        }

        private static async Task<StoreVm> CreateStoreAsync(HttpClient client, string name)
        {
            var response = await client.PostAsJsonAsync(Url, new AddOrUpdateStoreVm { Name = name });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<StoreVm>())!;
        }
    }
}
