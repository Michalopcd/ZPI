using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WebStore.Model.DataModels;
using WebStore.ViewModels.VM;

namespace WebStore.Tests.IntegrationTests
{
    public class ProductApiControllerIntegrationTests : BaseApiIntegrationTests
    {
        private const string Url = "/api/ProductApi";

        public ProductApiControllerIntegrationTests(WebStoreApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task GetAllProductsTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync(Url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var products = await response.Content.ReadFromJsonAsync<List<ProductVm>>();
            Assert.NotNull(products);
            Assert.Equal(QueryDatabase(db => db.Products.Count()), products!.Count);
            Assert.Contains(products, p => p.Name == "Monitor Dell 32");
        }

        [Fact]
        public async Task GetProductTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/2");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductVm>();
            Assert.Equal("Mysz Logitech", product!.Name);
            Assert.Equal(50, product.Quantity);
        }

        [Fact]
        public async Task GetNotExistingProductReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync($"{Url}/{NotExistingId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Contains($"Nie znaleziono produktu o Id {NotExistingId}.", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task AddProductTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productsCountBefore = QueryDatabase(db => db.Products.Count());

            var response = await client.PostAsJsonAsync(Url, CreateProductVm("Laptop Lenovo"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductVm>();
            Assert.Equal("Laptop Lenovo", product!.Name);
            Assert.Equal(productsCountBefore + 1, QueryDatabase(db => db.Products.Count()));
            Assert.True(QueryDatabase(db => db.Products.Any(p => p.Name == "Laptop Lenovo")));
        }

        [Fact]
        public async Task AddProductWithZeroIdTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productsCountBefore = QueryDatabase(db => db.Products.Count());
            var productVm = CreateProductVm("Słuchawki Sony");
            productVm.Id = 0;

            var response = await client.PostAsJsonAsync(Url, productVm);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductVm>();
            Assert.Equal("Słuchawki Sony", product!.Name);
            Assert.Equal(productsCountBefore + 1, QueryDatabase(db => db.Products.Count()));
            Assert.True(QueryDatabase(db => db.Products.Any(p => p.Name == "Słuchawki Sony")));
        }

        [Fact]
        public async Task AddProductWithExistingIdReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productId = AddProductToDatabase("Produkt bez zmian");
            var productsCountBefore = QueryDatabase(db => db.Products.Count());
            var productVm = CreateProductVm("Produkt nadpisany");
            productVm.Id = productId;

            var response = await client.PostAsJsonAsync(Url, productVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(productsCountBefore, QueryDatabase(db => db.Products.Count()));
            Assert.Equal("Produkt bez zmian", QueryDatabase(db => db.Products.Single(p => p.Id == productId).Name));
        }

        [Fact]
        public async Task AddProductWithNotExistingIdReturnsBadRequestTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productsCountBefore = QueryDatabase(db => db.Products.Count());
            var productVm = CreateProductVm("Produkt z obcym Id");
            productVm.Id = NotExistingId;

            var response = await client.PostAsJsonAsync(Url, productVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(productsCountBefore, QueryDatabase(db => db.Products.Count()));
            Assert.False(QueryDatabase(db => db.Products.Any(p => p.Name == "Produkt z obcym Id")));
        }

        [Fact]
        public async Task UpdateProductTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productId = AddProductToDatabase("Tablet do edycji");
            var productVm = CreateProductVm("Tablet po edycji");
            productVm.Id = productId;
            productVm.Price = 1234;

            var response = await client.PutAsJsonAsync(Url, productVm);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductVm>();
            Assert.Equal("Tablet po edycji", product!.Name);
            Assert.Equal(1234, product.Price);
            Assert.Equal("Tablet po edycji", QueryDatabase(db => db.Products.Single(p => p.Id == productId).Name));
        }

        [Fact]
        public async Task UpdateNotExistingProductReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productsCountBefore = QueryDatabase(db => db.Products.Count());
            var productVm = CreateProductVm("Nieistniejący produkt");
            productVm.Id = NotExistingId;

            var response = await client.PutAsJsonAsync(Url, productVm);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(productsCountBefore, QueryDatabase(db => db.Products.Count()));
            Assert.False(QueryDatabase(db => db.Products.Any(p => p.Name == "Nieistniejący produkt")));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        public async Task UpdateProductWithoutIdReturnsBadRequestTest(int? id)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productsCountBefore = QueryDatabase(db => db.Products.Count());
            var productVm = CreateProductVm("Produkt bez Id");
            productVm.Id = id;

            var response = await client.PutAsJsonAsync(Url, productVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(productsCountBefore, QueryDatabase(db => db.Products.Count()));
            Assert.False(QueryDatabase(db => db.Products.Any(p => p.Name == "Produkt bez Id")));
        }

        [Theory]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task AddOrUpdateProductWithInvalidModelReturnsBadRequestTest(string method)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            using var request = new HttpRequestMessage(new HttpMethod(method), Url)
            {
                Content = JsonContent.Create(new { price = 10, categoryId = 1, supplierId = 1 })
            };

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Name", body);
            Assert.Contains("Description", body);
            Assert.Contains("Nazwa produktu jest wymagana.", body);
            Assert.Contains("Opis produktu jest wymagany.", body);
            Assert.DoesNotContain("field is required", body);
        }

        [Theory]
        [InlineData("price", "Cena produktu jest wymagana.")]
        [InlineData("weight", "Waga produktu jest wymagana.")]
        [InlineData("categoryId", "Kategoria produktu jest wymagana.")]
        [InlineData("supplierId", "Dostawca produktu jest wymagany.")]
        public async Task AddProductWithoutRequiredValueReturnsBadRequestTest(string propertyName, string expectedMessage)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productsCountBefore = QueryDatabase(db => db.Products.Count());
            var productName = $"Produkt bez {propertyName}";

            var response = await client.PostAsync(Url, CreateJsonContentWithout(CreateProductVm(productName), propertyName));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
            Assert.Equal(productsCountBefore, QueryDatabase(db => db.Products.Count()));
            Assert.False(QueryDatabase(db => db.Products.Any(p => p.Name == productName)));
        }

        [Theory]
        [InlineData("price", "Cena produktu jest wymagana.")]
        [InlineData("weight", "Waga produktu jest wymagana.")]
        [InlineData("categoryId", "Kategoria produktu jest wymagana.")]
        [InlineData("supplierId", "Dostawca produktu jest wymagany.")]
        public async Task UpdateProductWithoutRequiredValueReturnsBadRequestTest(string propertyName, string expectedMessage)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productId = AddProductToDatabase("Produkt do niepełnej edycji");
            var productVm = CreateProductVm("Produkt po niepełnej edycji");
            productVm.Id = productId;

            var response = await client.PutAsync(Url, CreateJsonContentWithout(productVm, propertyName));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
            AssertProductUnchanged(productId, "Produkt do niepełnej edycji");
        }

        [Theory]
        [InlineData(NotExistingId, 1, "Nie znaleziono kategorii o Id 999999.")]
        [InlineData(1, NotExistingId, "Nie znaleziono dostawcy o Id 999999.")]
        public async Task AddProductWithNotExistingRelatedEntityReturnsBadRequestTest(int categoryId, int supplierId, string expectedMessage)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productsCountBefore = QueryDatabase(db => db.Products.Count());
            var productVm = CreateProductVm("Produkt z nieistniejącym powiązaniem");
            productVm.CategoryId = categoryId;
            productVm.SupplierId = supplierId;

            var response = await client.PostAsJsonAsync(Url, productVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
            Assert.Equal(productsCountBefore, QueryDatabase(db => db.Products.Count()));
        }

        [Theory]
        [InlineData(NotExistingId, 1, "Nie znaleziono kategorii o Id 999999.")]
        [InlineData(1, NotExistingId, "Nie znaleziono dostawcy o Id 999999.")]
        public async Task UpdateProductWithNotExistingRelatedEntityReturnsBadRequestTest(int categoryId, int supplierId, string expectedMessage)
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productId = AddProductToDatabase("Produkt z poprawnymi powiązaniami");
            var productVm = CreateProductVm("Produkt z błędnymi powiązaniami");
            productVm.Id = productId;
            productVm.CategoryId = categoryId;
            productVm.SupplierId = supplierId;

            var response = await client.PutAsJsonAsync(Url, productVm);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(expectedMessage, await response.Content.ReadAsStringAsync());
            AssertProductUnchanged(productId, "Produkt z poprawnymi powiązaniami");
        }

        [Fact]
        public async Task DeleteProductTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            var productId = AddProductToDatabase("Produkt do usunięcia");

            var response = await client.DeleteAsync($"{Url}/{productId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.False(QueryDatabase(db => db.Products.Any(p => p.Id == productId)));
        }

        [Fact]
        public async Task DeleteNotExistingProductReturnsNotFoundTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.DeleteAsync($"{Url}/{NotExistingId}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProductUsedInOrderIsNotSuccessfulTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();
            const int productId = 1;
            var orderProductsCountBefore = QueryDatabase(db => db.OrderProducts.Count(op => op.ProductId == productId));
            Assert.True(orderProductsCountBefore > 0);

            var response = await client.DeleteAsync($"{Url}/{productId}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Nie można usunąć produktu, ponieważ występuje w zamówieniach.", body);
            Assert.DoesNotContain("association", body);
            Assert.Contains(Factory.LoggerProvider.Entries, e => e.Level == LogLevel.Error
                && e.Exception?.InnerException is InvalidOperationException innerException
                && innerException.Message.Contains("association"));
            Assert.True(QueryDatabase(db => db.Products.Any(p => p.Id == productId)));
            Assert.Equal(orderProductsCountBefore, QueryDatabase(db => db.OrderProducts.Count(op => op.ProductId == productId)));
        }

        [Theory]
        [InlineData("GET", Url)]
        [InlineData("GET", Url + "/1")]
        [InlineData("POST", Url)]
        [InlineData("PUT", Url)]
        [InlineData("DELETE", Url + "/1")]
        public async Task RequestWithoutTokenReturnsUnauthorizedTest(string method, string url)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, await SendAnonymousRequestAsync(method, url));
        }

        private static AddOrUpdateProductVm CreateProductVm(string name)
        {
            return new AddOrUpdateProductVm
            {
                Name = name,
                Description = "Produkt testowy",
                ImageBytes = new byte[] { 0xff, 0x00, 0xff, 0x10 },
                Price = 999,
                Weight = 1.5f,
                CategoryId = 1,
                SupplierId = 1
            };
        }

        private void AssertProductUnchanged(int productId, string expectedName)
        {
            var product = QueryDatabase(db => db.Products.Single(p => p.Id == productId));
            Assert.Equal(expectedName, product.Name);
            Assert.Equal(500, product.Price);
            Assert.Equal(1, product.Weight);
            Assert.Equal(1, product.CategoryId);
            Assert.Equal(1, product.SupplierId);
        }

        private int AddProductToDatabase(string name)
        {
            return QueryDatabase(db =>
            {
                var product = new Product
                {
                    Name = name,
                    Description = "Produkt testowy",
                    ImageBytes = new byte[] { 0xff, 0x00, 0xff, 0x20 },
                    Price = 500,
                    Weight = 1,
                    CategoryId = 1,
                    SupplierId = 1
                };
                db.Products.Add(product);
                db.SaveChanges();
                return product.Id;
            });
        }
    }
}
