using System.Net;
using System.Text.Json;

namespace WebStore.Tests.IntegrationTests
{
    public class SwaggerIntegrationTests : BaseApiIntegrationTests
    {
        public SwaggerIntegrationTests(WebStoreApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task SwaggerUiIsAvailableTest()
        {
            using var client = Factory.CreateAnonymousClient();

            var response = await client.GetAsync("/swagger/index.html");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("swagger-ui", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SwaggerDocumentDescribesAllApiControllersTest()
        {
            using var client = Factory.CreateAnonymousClient();

            var response = await client.GetAsync("/swagger/v1/swagger.json");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;
            Assert.Equal("WebStore API", root.GetProperty("info").GetProperty("title").GetString());
            Assert.Equal("v1", root.GetProperty("info").GetProperty("version").GetString());

            var paths = root.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
            var expectedPaths = new[]
            {
                "/api/AccountApi/Login",
                "/api/ProductApi", "/api/ProductApi/{id}",
                "/api/OrderApi", "/api/OrderApi/{id}", "/api/OrderApi/{orderId}/Products/{productId}", "/api/OrderApi/Customer/{customerId}",
                "/api/InvoiceApi", "/api/InvoiceApi/{id}", "/api/InvoiceApi/Issue",
                "/api/StoreApi", "/api/StoreApi/{id}", "/api/StoreApi/{stationaryStoreId}/Employees/{employeeId}", "/api/StoreApi/{stationaryStoreId}/Addresses",
                "/api/AddressApi", "/api/AddressApi/{id}", "/api/AddressApi/Customer/{customerId}", "/api/AddressApi/Store/{stationaryStoreId}"
            };
            foreach (var expectedPath in expectedPaths)
                Assert.Contains(expectedPath, paths);

            var bearerScheme = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
            Assert.Equal("http", bearerScheme.GetProperty("type").GetString());
            Assert.Equal("bearer", bearerScheme.GetProperty("scheme").GetString());
            Assert.Equal("JWT", bearerScheme.GetProperty("bearerFormat").GetString());
            Assert.Contains(root.GetProperty("security").EnumerateArray(), requirement => requirement.TryGetProperty("Bearer", out _));
        }
    }
}
