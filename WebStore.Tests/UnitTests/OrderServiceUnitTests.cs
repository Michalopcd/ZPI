using WebStore.DAL.EF;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Tests.UnitTests
{
    public class OrderServiceUnitTests : BaseUnitTests
    {
        private readonly IOrderService _orderService;

        public OrderServiceUnitTests(ApplicationDbContext dbContext, IOrderService orderService)
            : base(dbContext)
        {
            _orderService = orderService;
        }

        [Fact]
        public void GetOrderTest()
        {
            var order = _orderService.GetOrder(o => o.Id == 1);
            Assert.NotNull(order);
            Assert.Equal(2, order.CustomerId);
            Assert.Equal(2, order.OrderProducts.Count);
        }

        [Fact]
        public void GetOrderWithNullFilterThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => _orderService.GetOrder(null!));
        }

        [Fact]
        public void GetAllOrdersTest()
        {
            var orders = _orderService.GetOrders();
            Assert.NotEmpty(orders);
            Assert.Equal(DbContext.Orders.Count(), orders.Count());
        }

        [Fact]
        public void GetFilteredOrdersTest()
        {
            var orders = _orderService.GetOrders(o => o.TrackingNumber == 1000000001).ToList();
            Assert.Single(orders);
            Assert.Equal(1, orders[0].Id);
        }

        [Fact]
        public void AddNewOrderTest()
        {
            var createdOrder = _orderService.AddOrUpdateOrder(CreateOrderVm(2000000001));
            Assert.NotNull(createdOrder);
            Assert.True(createdOrder.Id > 0);
            Assert.Equal(2, createdOrder.CustomerId);
            Assert.Equal(0, createdOrder.TotalAmount);
            Assert.NotNull(DbContext.Orders.Find(createdOrder.Id));
        }

        [Fact]
        public void UpdateOrderTest()
        {
            var createdOrder = _orderService.AddOrUpdateOrder(CreateOrderVm(2000000002));
            var updateOrderVm = CreateOrderVm(2000000003);
            updateOrderVm.Id = createdOrder.Id;
            updateOrderVm.DeliveryDate = new DateTime(2024, 2, 20);

            var updatedOrder = _orderService.AddOrUpdateOrder(updateOrderVm);
            Assert.Equal(createdOrder.Id, updatedOrder.Id);
            Assert.Equal(2000000003, updatedOrder.TrackingNumber);
            Assert.Equal(new DateTime(2024, 2, 20), updatedOrder.DeliveryDate);
        }

        [Fact]
        public void AddOrUpdateOrderWithNullThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => _orderService.AddOrUpdateOrder(null!));
        }

        [Fact]
        public void DeleteOrderTest()
        {
            var createdOrder = _orderService.AddOrUpdateOrder(CreateOrderVm(2000000004));
            _orderService.AddProductToOrder(createdOrder.Id, 2, 1);

            _orderService.DeleteOrder(createdOrder.Id);

            Assert.Null(_orderService.GetOrder(o => o.Id == createdOrder.Id));
            Assert.DoesNotContain(DbContext.OrderProducts, op => op.OrderId == createdOrder.Id);
        }

        [Fact]
        public void DeleteNotExistingOrderThrowsTest()
        {
            Assert.Throws<KeyNotFoundException>(() => _orderService.DeleteOrder(int.MaxValue));
        }

        [Fact]
        public void AddProductToOrderTest()
        {
            var createdOrder = _orderService.AddOrUpdateOrder(CreateOrderVm(2000000005));

            var orderWithNewProduct = _orderService.AddProductToOrder(createdOrder.Id, 2, 2);
            var orderProduct = Assert.Single(orderWithNewProduct.OrderProducts);
            Assert.Equal(2, orderProduct.ProductId);
            Assert.Equal(2, orderProduct.Quantity);

            var orderWithIncreasedQuantity = _orderService.AddProductToOrder(createdOrder.Id, 2, 1);
            orderProduct = Assert.Single(orderWithIncreasedQuantity.OrderProducts);
            Assert.Equal(3, orderProduct.Quantity);
        }

        [Fact]
        public void AddProductToOrderWithInvalidQuantityThrowsTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _orderService.AddProductToOrder(1, 1, 0));
        }

        [Fact]
        public void GetCustomerOrdersTest()
        {
            var orders = _orderService.GetCustomerOrders(2).ToList();
            Assert.NotEmpty(orders);
            Assert.All(orders, o => Assert.Equal(2, o.CustomerId));
            Assert.Contains(orders, o => o.Id == 1);
        }

        [Fact]
        public void OrderTotalAmountTest()
        {
            var createdOrder = _orderService.AddOrUpdateOrder(CreateOrderVm(2000000006));
            _orderService.AddProductToOrder(createdOrder.Id, 1, 2);
            var order = _orderService.AddProductToOrder(createdOrder.Id, 2, 3);

            var expectedTotalAmount = 2 * DbContext.Products.Find(1)!.Price + 3 * DbContext.Products.Find(2)!.Price;
            Assert.True(expectedTotalAmount > 0);
            Assert.Equal(expectedTotalAmount, order.TotalAmount);
            Assert.Equal(order.OrderProducts.Sum(op => op.Quantity * op.ProductPrice), order.TotalAmount);
        }

        [Fact]
        public void AddOrderWithNotExistingCustomerThrowsTest()
        {
            var ordersCountBefore = DbContext.Orders.Count();
            var orderVm = CreateOrderVm(2000000101);
            orderVm.CustomerId = int.MaxValue;

            var exception = Assert.Throws<ArgumentException>(() => _orderService.AddOrUpdateOrder(orderVm));

            Assert.Equal($"Nie znaleziono klienta o Id {int.MaxValue}.", exception.Message);
            Assert.Equal(ordersCountBefore, DbContext.Orders.Count());
        }

        [Fact]
        public void UpdateOrderWithNotExistingCustomerThrowsTest()
        {
            var createdOrder = _orderService.AddOrUpdateOrder(CreateOrderVm(2000000102));
            var orderVm = CreateOrderVm(2000000103);
            orderVm.Id = createdOrder.Id;
            orderVm.CustomerId = int.MaxValue;

            Assert.Throws<ArgumentException>(() => _orderService.AddOrUpdateOrder(orderVm));

            DbContext.ChangeTracker.Clear();
            var order = DbContext.Orders.Single(o => o.Id == createdOrder.Id);
            Assert.Equal(2, order.CustomerId);
            Assert.Equal(2000000102, order.TrackingNumber);
        }

        [Fact]
        public void AddOrderWithoutRequiredValuesThrowsTest()
        {
            var ordersCountBefore = DbContext.Orders.Count();

            var exception = Assert.Throws<ArgumentException>(() => _orderService.AddOrUpdateOrder(new AddOrUpdateOrderVm()));

            Assert.Equal("Brak wymaganych danych: klient, data zamówienia, data dostawy, numer przesyłki.", exception.Message);
            Assert.Equal(ordersCountBefore, DbContext.Orders.Count());
        }

        private static AddOrUpdateOrderVm CreateOrderVm(long trackingNumber)
        {
            return new AddOrUpdateOrderVm()
            {
                CustomerId = 2,
                OrderDate = new DateTime(2024, 2, 1),
                DeliveryDate = new DateTime(2024, 2, 5),
                TrackingNumber = trackingNumber
            };
        }
    }
}
