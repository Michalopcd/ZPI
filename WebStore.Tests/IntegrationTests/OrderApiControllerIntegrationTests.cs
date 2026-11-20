using System.Net;
using System.Net.Http.Json;
using WebStore.ViewModels.VM;

namespace WebStore.Tests.IntegrationTests
{
    public class OrderApiControllerIntegrationTests : BaseApiIntegrationTests
    {
        private const string Url = "/api/OrderApi";
        private const int CustomerId = 2;

        public OrderApiControllerIntegrationTests(WebStoreApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAllOrdersTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync(Url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var orders = await response.Content.ReadFromJsonAsync<List<OrderVm>>();
            Assert.Equal(QueryDatabase(db => db.Orders.Count()), orders!.Count);
            Assert.Contains(orders, o => o.Id == 1);
        }

        [Fact]
        public async Task GetOrderTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var order = await response.Content.ReadFromJsonAsync<OrderVm>();
            Assert.Equal(1, order!.Id);
            Assert.Equal(CustomerId, order.CustomerId);
            Assert.Equal(2, order.OrderProducts.Count);
            Assert.Equal(1 * 1000m + 2 * 500m, order.TotalAmount);
        }

        [Fact]
        public async Task GetNotExistingOrderReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/{NotExistingId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddOrderTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync(Url, CreateOrderVm(2000000001));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var order = await response.Content.ReadFromJsonAsync<OrderVm>();
            Assert.True(order!.Id > 0);
            Assert.Equal(2000000001, order.TrackingNumber);
            Assert.True(QueryDatabase(db => db.Orders.Any(o => o.Id == order.Id)));
        }

        [Fact]
        public async Task AddOrderWithZeroIdTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var ordersCountBefore = QueryDatabase(db => db.Orders.Count());
            var orderVm = CreateOrderVm(2000000009);
            orderVm.Id = 0;

            var response = await client.PostAsJsonAsync(Url, orderVm);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var order = await response.Content.ReadFromJsonAsync<OrderVm>();
            Assert.True(order!.Id > 0);
            Assert.Equal(ordersCountBefore + 1, QueryDatabase(db => db.Orders.Count()));
        }

        [Fact]
        public async Task AddOrderWithExistingIdReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdOrder = await CreateOrderAsync(client, 2000000010);
            var ordersCountBefore = QueryDatabase(db => db.Orders.Count());
            var orderVm = CreateOrderVm(2000000011);
            orderVm.Id = createdOrder.Id;

            var response = await client.PostAsJsonAsync(Url, orderVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(ordersCountBefore, QueryDatabase(db => db.Orders.Count()));
            Assert.Equal(2000000010, QueryDatabase(db => db.Orders.Single(o => o.Id == createdOrder.Id).TrackingNumber));
        }

        [Fact]
        public async Task AddOrderWithNotExistingIdReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var ordersCountBefore = QueryDatabase(db => db.Orders.Count());
            var orderVm = CreateOrderVm(2000000012);
            orderVm.Id = NotExistingId;

            var response = await client.PostAsJsonAsync(Url, orderVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(ordersCountBefore, QueryDatabase(db => db.Orders.Count()));
            Assert.False(QueryDatabase(db => db.Orders.Any(o => o.TrackingNumber == 2000000012)));
        }

        [Fact]
        public async Task UpdateOrderTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdOrder = await CreateOrderAsync(client, 2000000002);
            var orderVm = CreateOrderVm(2000000003);
            orderVm.Id = createdOrder.Id;

            var response = await client.PutAsJsonAsync(Url, orderVm);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var order = await response.Content.ReadFromJsonAsync<OrderVm>();
            Assert.Equal(createdOrder.Id, order!.Id);
            Assert.Equal(2000000003, order.TrackingNumber);
            Assert.Equal(2000000003, QueryDatabase(db => db.Orders.Single(o => o.Id == createdOrder.Id).TrackingNumber));
        }

        [Fact]
        public async Task UpdateNotExistingOrderReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var ordersCountBefore = QueryDatabase(db => db.Orders.Count());
            var orderVm = CreateOrderVm(2000000004);
            orderVm.Id = NotExistingId;

            var response = await client.PutAsJsonAsync(Url, orderVm);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(ordersCountBefore, QueryDatabase(db => db.Orders.Count()));
            Assert.False(QueryDatabase(db => db.Orders.Any(o => o.TrackingNumber == 2000000004)));
        }

        [Theory]
        [InlineData(null, 2000000013)]
        [InlineData(0, 2000000014)]
        public async Task UpdateOrderWithoutIdReturnsBadRequestTest(int? id, long trackingNumber)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var ordersCountBefore = QueryDatabase(db => db.Orders.Count());
            var orderVm = CreateOrderVm(trackingNumber);
            orderVm.Id = id;

            var response = await client.PutAsJsonAsync(Url, orderVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(ordersCountBefore, QueryDatabase(db => db.Orders.Count()));
            Assert.False(QueryDatabase(db => db.Orders.Any(o => o.TrackingNumber == trackingNumber)));
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task AddOrUpdateOrderWithInvalidModelReturnsBadRequestTest(string method)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            using var request = new HttpRequestMessage(new HttpMethod(method), Url)
            {
                Content = JsonContent.Create(new { customerId = "nie-liczba", orderDate = "nie-data" })
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Niepoprawny format danych.", body);
            Assert.DoesNotContain("Could not convert", body);
        }

        [Fact]
        public async Task AddOrderWithEmptyModelReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var ordersCountBefore = QueryDatabase(db => db.Orders.Count());

            var response = await client.PostAsJsonAsync(Url, new { });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Klient zamówienia jest wymagany.", body);
            Assert.Contains("Data zamówienia jest wymagana.", body);
            Assert.Contains("Data dostawy jest wymagana.", body);
            Assert.Contains("Numer przesyłki jest wymagany.", body);
            Assert.Equal(ordersCountBefore, QueryDatabase(db => db.Orders.Count()));
        }

        [Fact]
        public async Task UpdateOrderWithIdOnlyReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdOrder = await CreateOrderAsync(client, 2000000015);

            var response = await client.PutAsJsonAsync(Url, new { id = createdOrder.Id });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AssertOrderUnchanged(createdOrder.Id, 2000000015);
        }

        [Theory]
        [InlineData("customerId", "Klient zamówienia jest wymagany.", 2000000016)]
        [InlineData("orderDate", "Data zamówienia jest wymagana.", 2000000017)]
        [InlineData("deliveryDate", "Data dostawy jest wymagana.", 2000000018)]
        [InlineData("trackingNumber", "Numer przesyłki jest wymagany.", 2000000019)]
        public async Task AddOrderWithoutRequiredValueReturnsBadRequestTest(string propertyName, string expectedMessage, long trackingNumber)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var ordersCountBefore = QueryDatabase(db => db.Orders.Count());

            var response = await client.PostAsync(Url, CreateJsonContentWithout(CreateOrderVm(trackingNumber), propertyName));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
            Assert.Equal(ordersCountBefore, QueryDatabase(db => db.Orders.Count()));
        }

        [Theory]
        [InlineData("customerId", "Klient zamówienia jest wymagany.", 2000000020)]
        [InlineData("orderDate", "Data zamówienia jest wymagana.", 2000000021)]
        [InlineData("deliveryDate", "Data dostawy jest wymagana.", 2000000022)]
        [InlineData("trackingNumber", "Numer przesyłki jest wymagany.", 2000000023)]
        public async Task UpdateOrderWithoutRequiredValueReturnsBadRequestTest(string propertyName, string expectedMessage, long trackingNumber)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdOrder = await CreateOrderAsync(client, trackingNumber);
            var orderVm = CreateOrderVm(trackingNumber + 100);
            orderVm.Id = createdOrder.Id;
            orderVm.OrderDate = new DateTime(2025, 1, 1);
            orderVm.DeliveryDate = new DateTime(2025, 1, 2);

            var response = await client.PutAsync(Url, CreateJsonContentWithout(orderVm, propertyName));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
            AssertOrderUnchanged(createdOrder.Id, trackingNumber);
        }

        [Fact]
        public async Task AddOrderWithNotExistingCustomerReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var ordersCountBefore = QueryDatabase(db => db.Orders.Count());
            var orderVm = CreateOrderVm(2000000024);
            orderVm.CustomerId = NotExistingId;

            var response = await client.PostAsJsonAsync(Url, orderVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains($"Nie znaleziono klienta o Id {NotExistingId}.", await response.Content.ReadAsStringAsync());
            Assert.Equal(ordersCountBefore, QueryDatabase(db => db.Orders.Count()));
        }

        [Fact]
        public async Task UpdateOrderWithNotExistingCustomerReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdOrder = await CreateOrderAsync(client, 2000000025);
            var orderVm = CreateOrderVm(2000000026);
            orderVm.Id = createdOrder.Id;
            orderVm.CustomerId = NotExistingId;

            var response = await client.PutAsJsonAsync(Url, orderVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains($"Nie znaleziono klienta o Id {NotExistingId}.", await response.Content.ReadAsStringAsync());
            AssertOrderUnchanged(createdOrder.Id, 2000000025);
        }

        [Fact]
        public async Task DeleteOrderTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdOrder = await CreateOrderAsync(client, 2000000005);

            var response = await client.DeleteAsync($"{Url}/{createdOrder.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(QueryDatabase(db => db.Orders.Any(o => o.Id == createdOrder.Id)));
        }

        [Fact]
        public async Task DeleteNotExistingOrderReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.DeleteAsync($"{Url}/{NotExistingId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddProductToOrderTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdOrder = await CreateOrderAsync(client, 2000000006);

            var response = await client.PostAsync($"{Url}/{createdOrder.Id}/Products/2?quantity=3", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var order = await response.Content.ReadFromJsonAsync<OrderVm>();
            var orderProduct = Assert.Single(order!.OrderProducts);
            Assert.Equal(2, orderProduct.ProductId);
            Assert.Equal(3, orderProduct.Quantity);
            Assert.Equal(3 * 500m, order.TotalAmount);
        }

        [Fact]
        public async Task AddProductToNotExistingOrderReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsync($"{Url}/{NotExistingId}/Products/2?quantity=1", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddNotExistingProductToOrderReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdOrder = await CreateOrderAsync(client, 2000000007);

            var response = await client.PostAsync($"{Url}/{createdOrder.Id}/Products/{NotExistingId}?quantity=1", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("?quantity=0")]
        [InlineData("?quantity=-2")]
        [InlineData("")]
        public async Task AddProductToOrderWithInvalidQuantityReturnsBadRequestTest(string query)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdOrder = await CreateOrderAsync(client, 2000000008);

            var response = await client.PostAsync($"{Url}/{createdOrder.Id}/Products/2{query}", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Ilość produktu musi być większa od zera.", body);
            Assert.DoesNotContain("Parameter", body);
        }

        [Fact]
        public async Task GetCustomerOrdersTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/Customer/{CustomerId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var orders = await response.Content.ReadFromJsonAsync<List<OrderVm>>();
            Assert.Contains(orders!, o => o.Id == 1);
            Assert.All(orders!, o => Assert.Equal(CustomerId, o.CustomerId));
            Assert.Equal(QueryDatabase(db => db.Orders.Count(o => o.CustomerId == CustomerId)), orders!.Count);
        }

        [Theory]
        [InlineData("GET", Url)]
        [InlineData("GET", Url + "/1")]
        [InlineData("POST", Url)]
        [InlineData("PUT", Url)]
        [InlineData("DELETE", Url + "/1")]
        [InlineData("POST", Url + "/1/Products/1?quantity=1")]
        [InlineData("GET", Url + "/Customer/2")]
        public async Task RequestWithoutTokenReturnsUnauthorizedTest(string method, string url)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await SendAnonymousRequestAsync(method, url));
        }

        private static AddOrUpdateOrderVm CreateOrderVm(long trackingNumber)
        {
            return new AddOrUpdateOrderVm
            {
                CustomerId = CustomerId,
                OrderDate = new DateTime(2024, 5, 1),
                DeliveryDate = new DateTime(2024, 5, 5),
                TrackingNumber = trackingNumber
            };
        }

        private void AssertOrderUnchanged(int orderId, long expectedTrackingNumber)
        {
            var order = QueryDatabase(db => db.Orders.Single(o => o.Id == orderId));
            Assert.Equal(CustomerId, order.CustomerId);
            Assert.Equal(new DateTime(2024, 5, 1), order.OrderDate);
            Assert.Equal(new DateTime(2024, 5, 5), order.DeliveryDate);
            Assert.Equal(expectedTrackingNumber, order.TrackingNumber);
        }

        private static async Task<OrderVm> CreateOrderAsync(HttpClient client, long trackingNumber)
        {
            var response = await client.PostAsJsonAsync(Url, CreateOrderVm(trackingNumber));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<OrderVm>())!;
        }
    }
}
