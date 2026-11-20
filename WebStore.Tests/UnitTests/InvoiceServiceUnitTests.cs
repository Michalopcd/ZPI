using WebStore.DAL.EF;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Tests.UnitTests
{
    public class InvoiceServiceUnitTests : BaseUnitTests
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IOrderService _orderService;

        public InvoiceServiceUnitTests(ApplicationDbContext dbContext, IInvoiceService invoiceService, IOrderService orderService)
            : base(dbContext)
        {
            _invoiceService = invoiceService;
            _orderService = orderService;
        }

        [Fact]
        public void GetInvoiceTest()
        {
            var invoice = _invoiceService.GetInvoice(i => i.Id == 1);
            Assert.NotNull(invoice);
            Assert.Equal("FV/2023/0001", invoice.InvoiceNumber);
            Assert.Contains(1, invoice.OrderIds);
        }

        [Fact]
        public void GetInvoiceWithNullFilterThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => _invoiceService.GetInvoice(null!));
        }

        [Fact]
        public void GetAllInvoicesTest()
        {
            var invoices = _invoiceService.GetInvoices();
            Assert.NotEmpty(invoices);
            Assert.Equal(DbContext.Invoices.Count(), invoices.Count());
        }

        [Fact]
        public void GetFilteredInvoicesTest()
        {
            var invoices = _invoiceService.GetInvoices(i => i.InvoiceNumber == "FV/2023/0001").ToList();
            Assert.Single(invoices);
            Assert.Equal(1, invoices[0].Id);
        }

        [Fact]
        public void AddNewInvoiceTest()
        {
            var createdInvoice = _invoiceService.AddOrUpdateInvoice(CreateInvoiceVm("FV/TEST/0001"));
            Assert.NotNull(createdInvoice);
            Assert.True(createdInvoice.Id > 0);
            Assert.Equal("FV/TEST/0001", createdInvoice.InvoiceNumber);
            Assert.NotNull(DbContext.Invoices.Find(createdInvoice.Id));
        }

        [Fact]
        public void UpdateInvoiceTest()
        {
            var createdInvoice = _invoiceService.AddOrUpdateInvoice(CreateInvoiceVm("FV/TEST/0002"));
            var updateInvoiceVm = CreateInvoiceVm("FV/TEST/0003");
            updateInvoiceVm.Id = createdInvoice.Id;
            updateInvoiceVm.TotalAmount = 250;

            var updatedInvoice = _invoiceService.AddOrUpdateInvoice(updateInvoiceVm);
            Assert.Equal(createdInvoice.Id, updatedInvoice.Id);
            Assert.Equal("FV/TEST/0003", updatedInvoice.InvoiceNumber);
            Assert.Equal(250, updatedInvoice.TotalAmount);
        }

        [Fact]
        public void AddOrUpdateInvoiceWithNullThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => _invoiceService.AddOrUpdateInvoice(null!));
        }

        [Fact]
        public void DeleteInvoiceTest()
        {
            var orderId = CreateOrderWithProduct(3000000001, 1, 1);
            var issuedInvoice = _invoiceService.IssueInvoice(new[] { orderId });

            _invoiceService.DeleteInvoice(issuedInvoice.Id);

            Assert.Null(_invoiceService.GetInvoice(i => i.Id == issuedInvoice.Id));
            Assert.Null(DbContext.Orders.Find(orderId)!.InvoiceId);
        }

        [Fact]
        public void DeleteNotExistingInvoiceThrowsTest()
        {
            Assert.Throws<KeyNotFoundException>(() => _invoiceService.DeleteInvoice(int.MaxValue));
        }

        [Fact]
        public void IssueInvoiceTest()
        {
            var firstOrderId = CreateOrderWithProduct(3000000002, 1, 1);
            var secondOrderId = CreateOrderWithProduct(3000000003, 2, 4);
            var expectedTotalAmount = DbContext.Orders.Find(firstOrderId)!.TotalAmount
                + DbContext.Orders.Find(secondOrderId)!.TotalAmount;

            var issuedInvoice = _invoiceService.IssueInvoice(new[] { firstOrderId, secondOrderId });

            Assert.NotNull(issuedInvoice);
            Assert.True(issuedInvoice.Id > 0);
            Assert.StartsWith("FV/", issuedInvoice.InvoiceNumber);
            Assert.Equal(DateTime.Today, issuedInvoice.IssueDate.Date);
            Assert.True(expectedTotalAmount > 0);
            Assert.Equal(expectedTotalAmount, issuedInvoice.TotalAmount);
            Assert.Equal(new[] { firstOrderId, secondOrderId }, issuedInvoice.OrderIds.OrderBy(id => id));
            Assert.Equal(issuedInvoice.Id, DbContext.Orders.Find(firstOrderId)!.InvoiceId);
            Assert.Equal(issuedInvoice.Id, DbContext.Orders.Find(secondOrderId)!.InvoiceId);
        }

        [Fact]
        public void IssueInvoiceWithNullThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => _invoiceService.IssueInvoice(null!));
        }

        [Fact]
        public void IssueInvoiceWithoutOrdersThrowsTest()
        {
            Assert.Throws<ArgumentException>(() => _invoiceService.IssueInvoice(Array.Empty<int>()));
        }

        [Fact]
        public void IssueInvoiceForInvoicedOrderThrowsTest()
        {
            Assert.Throws<InvalidOperationException>(() => _invoiceService.IssueInvoice(new[] { 1 }));
        }

        private int CreateOrderWithProduct(long trackingNumber, int productId, int quantity)
        {
            var order = _orderService.AddOrUpdateOrder(new AddOrUpdateOrderVm()
            {
                CustomerId = 2,
                OrderDate = new DateTime(2024, 3, 1),
                DeliveryDate = new DateTime(2024, 3, 4),
                TrackingNumber = trackingNumber
            });
            _orderService.AddProductToOrder(order.Id, productId, quantity);
            return order.Id;
        }

        [Fact]
        public void UpdateInvoiceWithoutRequiredValuesThrowsTest()
        {
            var createdInvoice = _invoiceService.AddOrUpdateInvoice(CreateInvoiceVm("FV/TEST/0101"));
            var updateInvoiceVm = CreateInvoiceVm("FV/TEST/0102");
            updateInvoiceVm.Id = createdInvoice.Id;
            updateInvoiceVm.IssueDate = null;
            updateInvoiceVm.TotalAmount = null;

            var exception = Assert.Throws<ArgumentException>(() => _invoiceService.AddOrUpdateInvoice(updateInvoiceVm));

            Assert.Equal("Brak wymaganych danych: data wystawienia, kwota.", exception.Message);
            DbContext.ChangeTracker.Clear();
            var invoice = DbContext.Invoices.Single(i => i.Id == createdInvoice.Id);
            Assert.Equal("FV/TEST/0101", invoice.InvoiceNumber);
            Assert.Equal(100, invoice.TotalAmount);
        }

        private static AddOrUpdateInvoiceVm CreateInvoiceVm(string invoiceNumber)
        {
            return new AddOrUpdateInvoiceVm()
            {
                InvoiceNumber = invoiceNumber,
                IssueDate = new DateTime(2024, 3, 10),
                TotalAmount = 100
            };
        }
    }
}
