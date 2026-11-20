using System.Net;
using System.Net.Http.Json;
using System.Text;
using WebStore.ViewModels.VM;

namespace WebStore.Tests.IntegrationTests
{
    public class InvoiceApiControllerIntegrationTests : BaseApiIntegrationTests
    {
        private const string Url = "/api/InvoiceApi";
        private const string IssueUrl = Url + "/Issue";

        public InvoiceApiControllerIntegrationTests(WebStoreApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAllInvoicesTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync(Url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var invoices = await response.Content.ReadFromJsonAsync<List<InvoiceVm>>();
            Assert.Equal(QueryDatabase(db => db.Invoices.Count()), invoices!.Count);
            Assert.Contains(invoices, i => i.InvoiceNumber == "FV/2023/0001");
        }

        [Fact]
        public async Task GetInvoiceTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var invoice = await response.Content.ReadFromJsonAsync<InvoiceVm>();
            Assert.Equal("FV/2023/0001", invoice!.InvoiceNumber);
            Assert.Contains(1, invoice.OrderIds);
        }

        [Fact]
        public async Task GetNotExistingInvoiceReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/{NotExistingId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddInvoiceTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync(Url, CreateInvoiceVm("FV/2024/9001"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var invoice = await response.Content.ReadFromJsonAsync<InvoiceVm>();
            Assert.True(invoice!.Id > 0);
            Assert.Equal("FV/2024/9001", invoice.InvoiceNumber);
            Assert.True(QueryDatabase(db => db.Invoices.Any(i => i.Id == invoice.Id)));
        }

        [Fact]
        public async Task AddInvoiceWithZeroIdTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var invoicesCountBefore = QueryDatabase(db => db.Invoices.Count());
            var invoiceVm = CreateInvoiceVm("FV/2024/9005");
            invoiceVm.Id = 0;

            var response = await client.PostAsJsonAsync(Url, invoiceVm);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var invoice = await response.Content.ReadFromJsonAsync<InvoiceVm>();
            Assert.True(invoice!.Id > 0);
            Assert.Equal(invoicesCountBefore + 1, QueryDatabase(db => db.Invoices.Count()));
        }

        [Fact]
        public async Task AddInvoiceWithExistingIdReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdInvoice = await CreateInvoiceAsync(client, "FV/2024/9006");
            var invoicesCountBefore = QueryDatabase(db => db.Invoices.Count());
            var invoiceVm = CreateInvoiceVm("FV/2024/9007");
            invoiceVm.Id = createdInvoice.Id;

            var response = await client.PostAsJsonAsync(Url, invoiceVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(invoicesCountBefore, QueryDatabase(db => db.Invoices.Count()));
            Assert.Equal("FV/2024/9006", QueryDatabase(db => db.Invoices.Single(i => i.Id == createdInvoice.Id).InvoiceNumber));
        }

        [Fact]
        public async Task AddInvoiceWithNotExistingIdReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var invoicesCountBefore = QueryDatabase(db => db.Invoices.Count());
            var invoiceVm = CreateInvoiceVm("FV/2024/9008");
            invoiceVm.Id = NotExistingId;

            var response = await client.PostAsJsonAsync(Url, invoiceVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(invoicesCountBefore, QueryDatabase(db => db.Invoices.Count()));
            Assert.False(QueryDatabase(db => db.Invoices.Any(i => i.InvoiceNumber == "FV/2024/9008")));
        }

        [Fact]
        public async Task UpdateInvoiceTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdInvoice = await CreateInvoiceAsync(client, "FV/2024/9002");
            var invoiceVm = CreateInvoiceVm("FV/2024/9003");
            invoiceVm.Id = createdInvoice.Id;
            invoiceVm.TotalAmount = 321;

            var response = await client.PutAsJsonAsync(Url, invoiceVm);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var invoice = await response.Content.ReadFromJsonAsync<InvoiceVm>();
            Assert.Equal(createdInvoice.Id, invoice!.Id);
            Assert.Equal("FV/2024/9003", invoice.InvoiceNumber);
            Assert.Equal(321, invoice.TotalAmount);
        }

        [Fact]
        public async Task UpdateNotExistingInvoiceReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var invoicesCountBefore = QueryDatabase(db => db.Invoices.Count());
            var invoiceVm = CreateInvoiceVm("FV/2024/9004");
            invoiceVm.Id = NotExistingId;

            var response = await client.PutAsJsonAsync(Url, invoiceVm);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(invoicesCountBefore, QueryDatabase(db => db.Invoices.Count()));
            Assert.False(QueryDatabase(db => db.Invoices.Any(i => i.InvoiceNumber == "FV/2024/9004")));
        }

        [Theory]
        [InlineData(null, "FV/2024/9009")]
        [InlineData(0, "FV/2024/9010")]
        public async Task UpdateInvoiceWithoutIdReturnsBadRequestTest(int? id, string invoiceNumber)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var invoicesCountBefore = QueryDatabase(db => db.Invoices.Count());
            var invoiceVm = CreateInvoiceVm(invoiceNumber);
            invoiceVm.Id = id;

            var response = await client.PutAsJsonAsync(Url, invoiceVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(invoicesCountBefore, QueryDatabase(db => db.Invoices.Count()));
            Assert.False(QueryDatabase(db => db.Invoices.Any(i => i.InvoiceNumber == invoiceNumber)));
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task AddOrUpdateInvoiceWithInvalidModelReturnsBadRequestTest(string method)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            using var request = new HttpRequestMessage(new HttpMethod(method), Url)
            {
                Content = JsonContent.Create(new { issueDate = new DateTime(2024, 1, 1), totalAmount = 100 })
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("InvoiceNumber", body);
            Assert.Contains("Numer faktury jest wymagany.", body);
        }

        [Theory]
        [InlineData("issueDate", "Data wystawienia faktury jest wymagana.", "FV/2024/9101")]
        [InlineData("totalAmount", "Kwota faktury jest wymagana.", "FV/2024/9102")]
        public async Task AddInvoiceWithoutRequiredValueReturnsBadRequestTest(string propertyName, string expectedMessage, string invoiceNumber)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var invoicesCountBefore = QueryDatabase(db => db.Invoices.Count());

            var response = await client.PostAsync(Url, CreateJsonContentWithout(CreateInvoiceVm(invoiceNumber), propertyName));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
            Assert.Equal(invoicesCountBefore, QueryDatabase(db => db.Invoices.Count()));
            Assert.False(QueryDatabase(db => db.Invoices.Any(i => i.InvoiceNumber == invoiceNumber)));
        }

        [Theory]
        [InlineData("issueDate", "Data wystawienia faktury jest wymagana.", "FV/2024/9103")]
        [InlineData("totalAmount", "Kwota faktury jest wymagana.", "FV/2024/9104")]
        public async Task UpdateInvoiceWithoutRequiredValueReturnsBadRequestTest(string propertyName, string expectedMessage, string invoiceNumber)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdInvoice = await CreateInvoiceAsync(client, invoiceNumber);
            var invoiceVm = CreateInvoiceVm(invoiceNumber + "/zmiana");
            invoiceVm.Id = createdInvoice.Id;
            invoiceVm.IssueDate = new DateTime(2025, 1, 1);
            invoiceVm.TotalAmount = 999;

            var response = await client.PutAsync(Url, CreateJsonContentWithout(invoiceVm, propertyName));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
            var invoice = QueryDatabase(db => db.Invoices.Single(i => i.Id == createdInvoice.Id));
            Assert.Equal(invoiceNumber, invoice.InvoiceNumber);
            Assert.Equal(new DateTime(2024, 6, 1), invoice.IssueDate);
            Assert.Equal(100, invoice.TotalAmount);
        }

        [Fact]
        public async Task DeleteInvoiceTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var createdInvoice = await CreateInvoiceAsync(client, "FV/2024/9005");

            var response = await client.DeleteAsync($"{Url}/{createdInvoice.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(QueryDatabase(db => db.Invoices.Any(i => i.Id == createdInvoice.Id)));
        }

        [Fact]
        public async Task DeleteNotExistingInvoiceReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.DeleteAsync($"{Url}/{NotExistingId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task IssueInvoiceTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var orderId = await CreateOrderWithProductAsync(client, 3000000001);

            var response = await client.PostAsJsonAsync(IssueUrl, new[] { orderId });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var invoice = await response.Content.ReadFromJsonAsync<InvoiceVm>();
            Assert.True(invoice!.Id > 0);
            Assert.Equal(new List<int> { orderId }, invoice.OrderIds);
            Assert.Equal(2 * 500m, invoice.TotalAmount);
            Assert.StartsWith("FV/", invoice.InvoiceNumber);
            Assert.Equal(invoice.Id, QueryDatabase(db => db.Orders.Single(o => o.Id == orderId).InvoiceId));
        }

        [Fact]
        public async Task IssueInvoiceWithoutOrdersReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync(IssueUrl, Array.Empty<int>());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Faktura musi obejmować co najmniej jedno zamówienie.", body);
            Assert.DoesNotContain("Parameter", body);
        }

        [Fact]
        public async Task IssueInvoiceWithNullReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var invoicesCountBefore = QueryDatabase(db => db.Invoices.Count());

            var response = await client.PostAsync(IssueUrl, new StringContent("null", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Treść żądania jest wymagana.", body);
            Assert.Contains("Pole orderIds jest wymagane.", body);
            Assert.DoesNotContain("field is required", body);
            Assert.Equal(invoicesCountBefore, QueryDatabase(db => db.Invoices.Count()));
        }

        [Fact]
        public async Task IssueInvoiceForInvoicedOrderReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync(IssueUrl, new[] { 1 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task IssueInvoiceForNotExistingOrderReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.PostAsJsonAsync(IssueUrl, new[] { NotExistingId });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("GET", Url)]
        [InlineData("GET", Url + "/1")]
        [InlineData("POST", Url)]
        [InlineData("PUT", Url)]
        [InlineData("DELETE", Url + "/1")]
        [InlineData("POST", IssueUrl)]
        public async Task RequestWithoutTokenReturnsUnauthorizedTest(string method, string url)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await SendAnonymousRequestAsync(method, url));
        }

        private static AddOrUpdateInvoiceVm CreateInvoiceVm(string invoiceNumber)
        {
            return new AddOrUpdateInvoiceVm
            {
                InvoiceNumber = invoiceNumber,
                IssueDate = new DateTime(2024, 6, 1),
                TotalAmount = 100
            };
        }

        private static async Task<InvoiceVm> CreateInvoiceAsync(HttpClient client, string invoiceNumber)
        {
            var response = await client.PostAsJsonAsync(Url, CreateInvoiceVm(invoiceNumber));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<InvoiceVm>())!;
        }

        private static async Task<int> CreateOrderWithProductAsync(HttpClient client, long trackingNumber)
        {
            var orderResponse = await client.PostAsJsonAsync("/api/OrderApi", new AddOrUpdateOrderVm
            {
                CustomerId = 2,
                OrderDate = new DateTime(2024, 6, 1),
                DeliveryDate = new DateTime(2024, 6, 3),
                TrackingNumber = trackingNumber
            });
            orderResponse.EnsureSuccessStatusCode();
            var order = (await orderResponse.Content.ReadFromJsonAsync<OrderVm>())!;

            var productResponse = await client.PostAsync($"/api/OrderApi/{order.Id}/Products/2?quantity=2", null);
            productResponse.EnsureSuccessStatusCode();
            return order.Id;
        }
    }
}
