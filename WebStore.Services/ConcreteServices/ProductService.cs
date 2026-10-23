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

                Product productEntity;
                if (addOrUpdateProductVm.Id is null or 0)
                {
                    productEntity = Mapper.Map<Product>(addOrUpdateProductVm);
                    DbContext.Products.Add(productEntity);
                }
                else
                {
                    productEntity = DbContext.Products.Find(addOrUpdateProductVm.Id.Value)
                        ?? throw new KeyNotFoundException($"Nie znaleziono produktu o Id {addOrUpdateProductVm.Id.Value}.");
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
    }
}
