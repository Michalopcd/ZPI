using System.Net;
using System.Net.Http.Json;
using WebStore.ViewModels.VM;

namespace WebStore.Tests.IntegrationTests
{
    public class AddressApiControllerIntegrationTests : BaseApiIntegrationTests
    {
        private const string Url = "/api/AddressApi";
        private const int CustomerId = 2;
        private const int StoreId = 1;

        public AddressApiControllerIntegrationTests(WebStoreApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAllAddressesTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync(Url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var addresses = await response.Content.ReadFromJsonAsync<List<AddressVm>>();
            Assert.Equal(QueryDatabase(db => db.Addresses.Count()), addresses!.Count);
            Assert.Contains(addresses, a => a.Id == 1);
        }

        [Fact]
        public async Task GetAddressTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var address = await response.Content.ReadFromJsonAsync<AddressVm>();
            Assert.Equal(1, address!.Id);
            Assert.Equal("Akademicka", address.Street);
            Assert.Equal(CustomerId, address.CustomerId);
        }

        [Fact]
        public async Task GetNotExistingAddressReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/{NotExistingId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddAddressTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync(Url, CreateAddressVm("Wolności"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var address = await response.Content.ReadFromJsonAsync<AddressVm>();
            Assert.True(address!.Id > 0);
            Assert.Equal("Wolności", address.Street);
            Assert.True(QueryDatabase(db => db.Addresses.Any(a => a.Id == address.Id)));
        }

        [Fact]
        public async Task AddAddressWithZeroIdTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var addressesCountBefore = QueryDatabase(db => db.Addresses.Count());
            var addressVm = CreateAddressVm("Kościuszki");
            addressVm.Id = 0;

            var response = await client.PostAsJsonAsync(Url, addressVm);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var address = await response.Content.ReadFromJsonAsync<AddressVm>();
            Assert.True(address!.Id > 0);
            Assert.Equal(addressesCountBefore + 1, QueryDatabase(db => db.Addresses.Count()));
        }

        [Fact]
        public async Task AddAddressWithExistingIdReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdAddress = await CreateAddressAsync(client, "Piłsudskiego");
            var addressesCountBefore = QueryDatabase(db => db.Addresses.Count());
            var addressVm = CreateAddressVm("Sienkiewicza");
            addressVm.Id = createdAddress.Id;

            var response = await client.PostAsJsonAsync(Url, addressVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(addressesCountBefore, QueryDatabase(db => db.Addresses.Count()));
            Assert.Equal("Piłsudskiego", QueryDatabase(db => db.Addresses.Single(a => a.Id == createdAddress.Id).Street));
        }

        [Fact]
        public async Task AddAddressWithNotExistingIdReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var addressesCountBefore = QueryDatabase(db => db.Addresses.Count());
            var addressVm = CreateAddressVm("Obce Id");
            addressVm.Id = NotExistingId;

            var response = await client.PostAsJsonAsync(Url, addressVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(addressesCountBefore, QueryDatabase(db => db.Addresses.Count()));
            Assert.False(QueryDatabase(db => db.Addresses.Any(a => a.Street == "Obce Id")));
        }

        [Fact]
        public async Task UpdateAddressTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdAddress = await CreateAddressAsync(client, "Słowackiego");
            var addressVm = CreateAddressVm("Mickiewicza");
            addressVm.Id = createdAddress.Id;

            var response = await client.PutAsJsonAsync(Url, addressVm);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var address = await response.Content.ReadFromJsonAsync<AddressVm>();
            Assert.Equal(createdAddress.Id, address!.Id);
            Assert.Equal("Mickiewicza", address.Street);
            Assert.Equal("Mickiewicza", QueryDatabase(db => db.Addresses.Single(a => a.Id == createdAddress.Id).Street));
        }

        [Fact]
        public async Task UpdateNotExistingAddressReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var addressesCountBefore = QueryDatabase(db => db.Addresses.Count());
            var addressVm = CreateAddressVm("Brak");
            addressVm.Id = NotExistingId;

            var response = await client.PutAsJsonAsync(Url, addressVm);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(addressesCountBefore, QueryDatabase(db => db.Addresses.Count()));
            Assert.False(QueryDatabase(db => db.Addresses.Any(a => a.Street == "Brak")));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        public async Task UpdateAddressWithoutIdReturnsBadRequestTest(int? id)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var addressesCountBefore = QueryDatabase(db => db.Addresses.Count());
            var addressVm = CreateAddressVm("Bez Id");
            addressVm.Id = id;

            var response = await client.PutAsJsonAsync(Url, addressVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(addressesCountBefore, QueryDatabase(db => db.Addresses.Count()));
            Assert.False(QueryDatabase(db => db.Addresses.Any(a => a.Street == "Bez Id")));
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task AddOrUpdateAddressWithInvalidModelReturnsBadRequestTest(string method)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            using var request = new HttpRequestMessage(new HttpMethod(method), Url)
            {
                Content = JsonContent.Create(new { street = "Bez reszty pól" })
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("City", body);
            Assert.Contains("PostalCode", body);
            Assert.Contains("Miasto jest wymagane.", body);
            Assert.Contains("Kod pocztowy jest wymagany.", body);
        }

        [Theory]
        [InlineData(NotExistingId, null, "Nie znaleziono klienta o Id 999999.")]
        [InlineData(null, NotExistingId, "Nie znaleziono sklepu o Id 999999.")]
        public async Task AddAddressWithNotExistingRelatedEntityReturnsBadRequestTest(int? customerId, int? stationaryStoreId, string expectedMessage)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var addressesCountBefore = QueryDatabase(db => db.Addresses.Count());
            var addressVm = CreateAddressVm("Ulica bez powiązań");
            addressVm.CustomerId = customerId;
            addressVm.StationaryStoreId = stationaryStoreId;

            var response = await client.PostAsJsonAsync(Url, addressVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
            Assert.Equal(addressesCountBefore, QueryDatabase(db => db.Addresses.Count()));
        }

        [Theory]
        [InlineData(NotExistingId, null, "Nie znaleziono klienta o Id 999999.")]
        [InlineData(null, NotExistingId, "Nie znaleziono sklepu o Id 999999.")]
        public async Task UpdateAddressWithNotExistingRelatedEntityReturnsBadRequestTest(int? customerId, int? stationaryStoreId, string expectedMessage)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdAddress = await CreateAddressAsync(client, "Ulica przed edycją");
            var addressVm = CreateAddressVm("Ulica po edycji");
            addressVm.Id = createdAddress.Id;
            addressVm.CustomerId = customerId;
            addressVm.StationaryStoreId = stationaryStoreId;

            var response = await client.PutAsJsonAsync(Url, addressVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
            var address = QueryDatabase(db => db.Addresses.Single(a => a.Id == createdAddress.Id));
            Assert.Equal("Ulica przed edycją", address.Street);
            Assert.Null(address.CustomerId);
            Assert.Null(address.StationaryStoreId);
        }

        [Fact]
        public async Task DeleteAddressTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdAddress = await CreateAddressAsync(client, "Do usunięcia");

            var response = await client.DeleteAsync($"{Url}/{createdAddress.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(QueryDatabase(db => db.Addresses.Any(a => a.Id == createdAddress.Id)));
        }

        [Fact]
        public async Task DeleteNotExistingAddressReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.DeleteAsync($"{Url}/{NotExistingId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCustomerAddressesTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/Customer/{CustomerId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var addresses = await response.Content.ReadFromJsonAsync<List<AddressVm>>();
            Assert.Contains(addresses!, a => a.Id == 1);
            Assert.Contains(addresses!, a => a.Id == 2);
            Assert.All(addresses!, a => Assert.Equal(CustomerId, a.CustomerId));
        }

        [Fact]
        public async Task GetStoreAddressesTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/Store/{StoreId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var addresses = await response.Content.ReadFromJsonAsync<List<AddressVm>>();
            Assert.Contains(addresses!, a => a.Id == 3);
            Assert.All(addresses!, a => Assert.Equal(StoreId, a.StationaryStoreId));
        }

        [Theory]
        [InlineData("GET", Url)]
        [InlineData("GET", Url + "/1")]
        [InlineData("POST", Url)]
        [InlineData("PUT", Url)]
        [InlineData("DELETE", Url + "/1")]
        [InlineData("GET", Url + "/Customer/2")]
        [InlineData("GET", Url + "/Store/1")]
        public async Task RequestWithoutTokenReturnsUnauthorizedTest(string method, string url)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await SendAnonymousRequestAsync(method, url));
        }

        private static AddOrUpdateAddressVm CreateAddressVm(string street)
        {
            return new AddOrUpdateAddressVm
            {
                Street = street,
                BuildingNumber = "7",
                PostalCode = "41-800",
                City = "Zabrze",
                Country = "Polska"
            };
        }

        private static async Task<AddressVm> CreateAddressAsync(HttpClient client, string street)
        {
            var response = await client.PostAsJsonAsync(Url, CreateAddressVm(street));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AddressVm>())!;
        }
    }
}
