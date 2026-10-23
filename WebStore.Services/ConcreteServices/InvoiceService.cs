using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Services.ConcreteServices
{
    public class InvoiceService : BaseService, IInvoiceService
    {
        public InvoiceService(ApplicationDbContext dbContext, IMapper mapper, ILogger logger)
            : base(dbContext, mapper, logger)
        {
        }

        public InvoiceVm AddOrUpdateInvoice(AddOrUpdateInvoiceVm addOrUpdateInvoiceVm)
        {
            try
            {
                if (addOrUpdateInvoiceVm == null)
                    throw new ArgumentNullException(nameof(addOrUpdateInvoiceVm), "Parametr view modelu jest pusty (null).");

                Invoice invoiceEntity;
                if (addOrUpdateInvoiceVm.Id is null or 0)
                {
                    invoiceEntity = Mapper.Map<Invoice>(addOrUpdateInvoiceVm);
                    DbContext.Invoices.Add(invoiceEntity);
                }
                else
                {
                    invoiceEntity = DbContext.Invoices.Find(addOrUpdateInvoiceVm.Id.Value)
                        ?? throw new KeyNotFoundException($"Nie znaleziono faktury o Id {addOrUpdateInvoiceVm.Id.Value}.");
                    Mapper.Map(addOrUpdateInvoiceVm, invoiceEntity);
                }

                DbContext.SaveChanges();
                return Mapper.Map<InvoiceVm>(invoiceEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public InvoiceVm GetInvoice(Expression<Func<Invoice, bool>> filterExpression)
        {
            try
            {
                if (filterExpression == null)
                    throw new ArgumentNullException(nameof(filterExpression), "Parametr wyrażenia filtrującego jest pusty (null).");

                var invoiceEntity = DbContext.Invoices.FirstOrDefault(filterExpression);
                return Mapper.Map<InvoiceVm>(invoiceEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public IEnumerable<InvoiceVm> GetInvoices(Expression<Func<Invoice, bool>>? filterExpression = null)
        {
            try
            {
                var invoicesQuery = DbContext.Invoices.AsQueryable();
                if (filterExpression != null)
                    invoicesQuery = invoicesQuery.Where(filterExpression);

                return Mapper.Map<IEnumerable<InvoiceVm>>(invoicesQuery);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public void DeleteInvoice(int id)
        {
            try
            {
                var invoiceEntity = DbContext.Invoices.Find(id)
                    ?? throw new KeyNotFoundException($"Nie znaleziono faktury o Id {id}.");

                foreach (var order in invoiceEntity.Orders.ToList())
                    order.InvoiceId = null;

                DbContext.Invoices.Remove(invoiceEntity);
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public InvoiceVm IssueInvoice(IEnumerable<int> orderIds)
        {
            try
            {
                if (orderIds == null)
                    throw new ArgumentNullException(nameof(orderIds), "Lista identyfikatorów zamówień jest pusta (null).");

                var distinctOrderIds = orderIds.Distinct().ToList();
                if (distinctOrderIds.Count == 0)
                    throw new ArgumentException("Faktura musi obejmować co najmniej jedno zamówienie.", nameof(orderIds));

                var orders = DbContext.Orders.Where(o => distinctOrderIds.Contains(o.Id)).ToList();
                var missingOrderIds = distinctOrderIds.Except(orders.Select(o => o.Id)).ToList();
                if (missingOrderIds.Count > 0)
                    throw new KeyNotFoundException($"Nie znaleziono zamówień o Id: {string.Join(", ", missingOrderIds)}.");

                var invoicedOrderIds = orders.Where(o => o.InvoiceId != null).Select(o => o.Id).ToList();
                if (invoicedOrderIds.Count > 0)
                    throw new InvalidOperationException($"Zamówienia o Id: {string.Join(", ", invoicedOrderIds)} mają już wystawioną fakturę.");

                var issueDate = DateTime.Now;
                var invoiceEntity = new Invoice
                {
                    IssueDate = issueDate,
                    InvoiceNumber = GenerateInvoiceNumber(issueDate),
                    TotalAmount = orders.Sum(o => o.TotalAmount)
                };
                foreach (var order in orders)
                    invoiceEntity.Orders.Add(order);

                DbContext.Invoices.Add(invoiceEntity);
                DbContext.SaveChanges();
                return Mapper.Map<InvoiceVm>(invoiceEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        private string GenerateInvoiceNumber(DateTime issueDate)
        {
            var sequenceNumber = DbContext.Invoices.Count(i => i.IssueDate.Year == issueDate.Year) + 1;
            return $"FV/{issueDate:yyyy}/{sequenceNumber:D4}";
        }
    }
}
