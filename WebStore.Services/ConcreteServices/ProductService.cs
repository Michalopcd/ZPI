using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Services.ConcreteServices
{
    public class ProductService : BaseService, IProductService
    {
        public ProductService(ApplicationDbContext dbContext, IMapper mapper, ILogger logger)
            : base(dbContext, mapper, logger)
        {
        }

        public ProductVm AddOrUpdateProduct(AddOrUpdateProductVm addOrUpdateProductVm)
        {
            try
            {
                if (addOrUpdateProductVm == null)
                    throw new ArgumentNullException(nameof(addOrUpdateProductVm), "Parametr view modelu jest pusty (null).");

                EnsureRequiredValues(
                    (addOrUpdateProductVm.Price, "cena"),
                    (addOrUpdateProductVm.Weight, "waga"),
                    (addOrUpdateProductVm.CategoryId, "kategoria"),
                    (addOrUpdateProductVm.SupplierId, "dostawca"));

                Product productEntity;
                if (addOrUpdateProductVm.Id is null or 0)
                {
                    EnsureRelatedEntitiesExist(addOrUpdateProductVm);
                    productEntity = Mapper.Map<Product>(addOrUpdateProductVm);
                    DbContext.Products.Add(productEntity);
                }
                else
                {
                    productEntity = DbContext.Products.Find(addOrUpdateProductVm.Id.Value)
                        ?? throw new KeyNotFoundException($"Nie znaleziono produktu o Id {addOrUpdateProductVm.Id.Value}.");
                    EnsureRelatedEntitiesExist(addOrUpdateProductVm);
                    Mapper.Map(addOrUpdateProductVm, productEntity);
                }

                DbContext.SaveChanges();
                return Mapper.Map<ProductVm>(productEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public ProductVm GetProduct(Expression<Func<Product, bool>> filterExpression)
        {
            try
            {
                if (filterExpression == null)
                    throw new ArgumentNullException(nameof(filterExpression), "Parametr wyrażenia filtrującego jest pusty (null).");

                var productEntity = DbContext.Products.FirstOrDefault(filterExpression);
                return Mapper.Map<ProductVm>(productEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public IEnumerable<ProductVm> GetProducts(Expression<Func<Product, bool>>? filterExpression = null)
        {
            try
            {
                var productsQuery = DbContext.Products.AsQueryable();
                if (filterExpression != null)
                    productsQuery = productsQuery.Where(filterExpression);

                return Mapper.Map<IEnumerable<ProductVm>>(productsQuery);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public void DeleteProduct(int id)
        {
            try
            {
                var productEntity = DbContext.Products.Find(id)
                    ?? throw new KeyNotFoundException($"Nie znaleziono produktu o Id {id}.");

                DbContext.Entry(productEntity).Collection(p => p.OrderProducts).Load();

                DbContext.ProductStocks.RemoveRange(productEntity.ProductStocks);
                try
                {
                    DbContext.Products.Remove(productEntity);
                    DbContext.SaveChanges();
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException("Nie można usunąć produktu, ponieważ występuje w zamówieniach.", ex);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        private void EnsureRelatedEntitiesExist(AddOrUpdateProductVm addOrUpdateProductVm)
        {
            if (!DbContext.Categories.Any(c => c.Id == addOrUpdateProductVm.CategoryId))
                throw new ArgumentException($"Nie znaleziono kategorii o Id {addOrUpdateProductVm.CategoryId}.");
            if (!DbContext.Suppliers.Any(s => s.Id == addOrUpdateProductVm.SupplierId))
                throw new ArgumentException($"Nie znaleziono dostawcy o Id {addOrUpdateProductVm.SupplierId}.");
        }
    }
}
