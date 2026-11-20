using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Services.ConcreteServices
{
    public class OrderService : BaseService, IOrderService
    {
        public OrderService(ApplicationDbContext dbContext, IMapper mapper, ILogger logger)
            : base(dbContext, mapper, logger)
        {
        }

        public OrderVm AddOrUpdateOrder(AddOrUpdateOrderVm addOrUpdateOrderVm)
        {
            try
            {
                if (addOrUpdateOrderVm == null)
                    throw new ArgumentNullException(nameof(addOrUpdateOrderVm), "Parametr view modelu jest pusty (null).");

                EnsureRequiredValues(
                    (addOrUpdateOrderVm.CustomerId, "klient"),
                    (addOrUpdateOrderVm.OrderDate, "data zamówienia"),
                    (addOrUpdateOrderVm.DeliveryDate, "data dostawy"),
                    (addOrUpdateOrderVm.TrackingNumber, "numer przesyłki"));

                Order orderEntity;
                if (addOrUpdateOrderVm.Id is null or 0)
                {
                    EnsureCustomerExists(addOrUpdateOrderVm.CustomerId!.Value);
                    orderEntity = Mapper.Map<Order>(addOrUpdateOrderVm);
                    DbContext.Orders.Add(orderEntity);
                }
                else
                {
                    orderEntity = DbContext.Orders.Find(addOrUpdateOrderVm.Id.Value)
                        ?? throw new KeyNotFoundException($"Nie znaleziono zamówienia o Id {addOrUpdateOrderVm.Id.Value}.");
                    EnsureCustomerExists(addOrUpdateOrderVm.CustomerId!.Value);
                    Mapper.Map(addOrUpdateOrderVm, orderEntity);
                }

                DbContext.SaveChanges();
                return Mapper.Map<OrderVm>(orderEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public OrderVm GetOrder(Expression<Func<Order, bool>> filterExpression)
        {
            try
            {
                if (filterExpression == null)
                    throw new ArgumentNullException(nameof(filterExpression), "Parametr wyrażenia filtrującego jest pusty (null).");

                var orderEntity = DbContext.Orders.FirstOrDefault(filterExpression);
                return Mapper.Map<OrderVm>(orderEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public IEnumerable<OrderVm> GetOrders(Expression<Func<Order, bool>>? filterExpression = null)
        {
            try
            {
                var ordersQuery = DbContext.Orders.AsQueryable();
                if (filterExpression != null)
                    ordersQuery = ordersQuery.Where(filterExpression);

                return Mapper.Map<IEnumerable<OrderVm>>(ordersQuery);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public void DeleteOrder(int id)
        {
            try
            {
                var orderEntity = DbContext.Orders.Find(id)
                    ?? throw new KeyNotFoundException($"Nie znaleziono zamówienia o Id {id}.");

                DbContext.OrderProducts.RemoveRange(orderEntity.OrderProducts);
                DbContext.Orders.Remove(orderEntity);
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public OrderVm AddProductToOrder(int orderId, int productId, int quantity)
        {
            try
            {
                if (quantity <= 0)
                    throw new ArgumentOutOfRangeException(nameof(quantity), "Ilość produktu musi być większa od zera.");

                var orderEntity = DbContext.Orders.Find(orderId)
                    ?? throw new KeyNotFoundException($"Nie znaleziono zamówienia o Id {orderId}.");
                var productEntity = DbContext.Products.Find(productId)
                    ?? throw new KeyNotFoundException($"Nie znaleziono produktu o Id {productId}.");

                var orderProduct = orderEntity.OrderProducts.FirstOrDefault(op => op.ProductId == productId);
                if (orderProduct == null)
                {
                    orderEntity.OrderProducts.Add(new OrderProduct
                    {
                        Order = orderEntity,
                        Product = productEntity,
                        Quantity = quantity
                    });
                }
                else
                {
                    orderProduct.Quantity += quantity;
                }

                DbContext.SaveChanges();
                return Mapper.Map<OrderVm>(orderEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public IEnumerable<OrderVm> GetCustomerOrders(int customerId)
        {
            return GetOrders(o => o.CustomerId == customerId);
        }

        private void EnsureCustomerExists(int customerId)
        {
            if (!DbContext.Customers.Any(c => c.Id == customerId))
                throw new ArgumentException($"Nie znaleziono klienta o Id {customerId}.");
        }
    }
}
