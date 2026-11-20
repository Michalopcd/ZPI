using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Tests.UnitTests
{
    public class ProductServiceUnitTests : BaseUnitTests
    {
        private readonly IProductService _productService;

        public ProductServiceUnitTests(ApplicationDbContext dbContext, IProductService productService)
            : base(dbContext)
        {
            _productService = productService;
        }

        [Fact]
        public void GetProductTest()
        {
            var product = _productService.GetProduct(p => p.Name == "Monitor Dell 32");
            Assert.NotNull(product);
        }

        [Fact]
        public void GetMultipleProductsTest()
        {
            var products = _productService.GetProducts(p => p.Id >= 1 && p.Id <= 2);
            Assert.NotNull(products);
            Assert.NotEmpty(products);
            Assert.Equal(2, products.Count());
        }

        [Fact]
        public void GetAllProductsTest()
        {
            var products = _productService.GetProducts();
            Assert.NotNull(products);
            Assert.NotEmpty(products);
            Assert.Equal(DbContext.Products.Count(), products.Count());
        }

        [Fact]
        public void AddNewProductTest()
        {
            var newProductVm = new AddOrUpdateProductVm()
            {
                Name = "MacBook Pro",
                CategoryId = 1,
                SupplierId = 1,
                ImageBytes = new byte[] { 0xff, 0xff, 0xff, 0x80 },
                Price = 6000,
                Weight = 1.1f,
                Description = "MacBook Pro z procesorem M1 8GB RAM, Dysk 256 GB"
            };
            var createdProduct = _productService.AddOrUpdateProduct(newProductVm);
            Assert.NotNull(createdProduct);
            Assert.Equal("MacBook Pro", createdProduct.Name);
        }

        [Fact]
        public void UpdateProductTest()
        {
            var updateProductVm = new AddOrUpdateProductVm()
            {
                Id = 1,
                Description = "Bardzo praktyczny monitor 32 cale",
                ImageBytes = new byte[] { 0xff, 0xff, 0xff, 0x80 },
                Name = "Monitor Dell 32",
                Price = 2000,
                Weight = 20,
                CategoryId = 1,
                SupplierId = 1
            };
            var editedProductVm = _productService.AddOrUpdateProduct(updateProductVm);
            Assert.NotNull(editedProductVm);
            Assert.Equal("Monitor Dell 32", editedProductVm.Name);
            Assert.Equal(2000, editedProductVm.Price);
        }

        [Fact]
        public void DeleteProductTest()
        {
            var productToDelete = new Product()
            {
                Name = "Klawiatura do usunięcia",
                CategoryId = 1,
                SupplierId = 1,
                ImageBytes = new byte[] { 0xff, 0xff, 0xff, 0x60 },
                Price = 150,
                Weight = 0.8f,
                Description = "Klawiatura mechaniczna",
                ProductStocks = new List<ProductStock> { new ProductStock() { Quantity = 5 } }
            };
            DbContext.Products.Add(productToDelete);
            DbContext.SaveChanges();
            var productId = productToDelete.Id;

            _productService.DeleteProduct(productId);

            Assert.False(DbContext.Products.Any(p => p.Id == productId));
            Assert.False(DbContext.ProductStocks.Any(ps => ps.ProductId == productId));
        }

        [Fact]
        public void DeleteNotExistingProductThrowsTest()
        {
            Assert.Throws<KeyNotFoundException>(() => _productService.DeleteProduct(int.MaxValue));
        }

        [Fact]
        public void DeleteProductUsedInOrderThrowsTest()
        {
            var orderProductsCountBefore = DbContext.OrderProducts.Count(op => op.ProductId == 1);
            Assert.True(orderProductsCountBefore > 0);

            var exception = Assert.Throws<InvalidOperationException>(() => _productService.DeleteProduct(1));

            Assert.Equal("Nie można usunąć produktu, ponieważ występuje w zamówieniach.", exception.Message);
            Assert.IsType<InvalidOperationException>(exception.InnerException);
            DbContext.ChangeTracker.Clear();
            Assert.True(DbContext.Products.Any(p => p.Id == 1));
            Assert.Equal(orderProductsCountBefore, DbContext.OrderProducts.Count(op => op.ProductId == 1));
        }

        [Theory]
        [InlineData(int.MaxValue, 1, "Nie znaleziono kategorii o Id 2147483647.")]
        [InlineData(1, int.MaxValue, "Nie znaleziono dostawcy o Id 2147483647.")]
        public void AddProductWithNotExistingRelatedEntityThrowsTest(int categoryId, int supplierId, string expectedMessage)
        {
            var productsCountBefore = DbContext.Products.Count();
            var productVm = CreateProductVm("Produkt bez powiązań");
            productVm.CategoryId = categoryId;
            productVm.SupplierId = supplierId;

            var exception = Assert.Throws<ArgumentException>(() => _productService.AddOrUpdateProduct(productVm));

            Assert.Equal(expectedMessage, exception.Message);
            Assert.Equal(productsCountBefore, DbContext.Products.Count());
        }

        [Theory]
        [InlineData(int.MaxValue, 1)]
        [InlineData(1, int.MaxValue)]
        public void UpdateProductWithNotExistingRelatedEntityThrowsTest(int categoryId, int supplierId)
        {
            var createdProduct = new Product()
            {
                Name = "Produkt przed edycją",
                CategoryId = 1,
                SupplierId = 1,
                ImageBytes = new byte[] { 0xff, 0xff, 0xff, 0x40 },
                Price = 300,
                Weight = 0.4f,
                Description = "Produkt testowy"
            };
            DbContext.Products.Add(createdProduct);
            DbContext.SaveChanges();
            var productVm = CreateProductVm("Produkt po edycji");
            productVm.Id = createdProduct.Id;
            productVm.CategoryId = categoryId;
            productVm.SupplierId = supplierId;

            Assert.Throws<ArgumentException>(() => _productService.AddOrUpdateProduct(productVm));

            DbContext.ChangeTracker.Clear();
            var product = DbContext.Products.Single(p => p.Id == createdProduct.Id);
            Assert.Equal("Produkt przed edycją", product.Name);
            Assert.Equal(1, product.CategoryId);
            Assert.Equal(1, product.SupplierId);
        }

        [Fact]
        public void AddProductWithoutRequiredValuesThrowsTest()
        {
            var productsCountBefore = DbContext.Products.Count();
            var productVm = CreateProductVm("Produkt bez ceny");
            productVm.Price = null;
            productVm.CategoryId = null;

            var exception = Assert.Throws<ArgumentException>(() => _productService.AddOrUpdateProduct(productVm));

            Assert.Equal("Brak wymaganych danych: cena, kategoria.", exception.Message);
            Assert.Equal(productsCountBefore, DbContext.Products.Count());
        }

        private static AddOrUpdateProductVm CreateProductVm(string name)
        {
            return new AddOrUpdateProductVm()
            {
                Name = name,
                CategoryId = 1,
                SupplierId = 1,
                ImageBytes = new byte[] { 0xff, 0xff, 0xff, 0x50 },
                Price = 300,
                Weight = 0.4f,
                Description = "Produkt testowy"
            };
        }
    }
}
