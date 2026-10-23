using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Services.ConcreteServices
{
    public class AddressService : BaseService, IAddressService
    {
        public AddressService(ApplicationDbContext dbContext, IMapper mapper, ILogger logger)
            : base(dbContext, mapper, logger)
        {
        }

        public AddressVm AddOrUpdateAddress(AddOrUpdateAddressVm addOrUpdateAddressVm)
        {
            try
            {
                if (addOrUpdateAddressVm == null)
                    throw new ArgumentNullException(nameof(addOrUpdateAddressVm), "Parametr view modelu jest pusty (null).");

                Address addressEntity;
                if (addOrUpdateAddressVm.Id is null or 0)
                {
                    addressEntity = Mapper.Map<Address>(addOrUpdateAddressVm);
                    DbContext.Addresses.Add(addressEntity);
                }
                else
                {
                    addressEntity = DbContext.Addresses.Find(addOrUpdateAddressVm.Id.Value)
                        ?? throw new KeyNotFoundException($"Nie znaleziono adresu o Id {addOrUpdateAddressVm.Id.Value}.");
                    Mapper.Map(addOrUpdateAddressVm, addressEntity);
                }

                DbContext.SaveChanges();
                return Mapper.Map<AddressVm>(addressEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public AddressVm GetAddress(Expression<Func<Address, bool>> filterExpression)
        {
            try
            {
                if (filterExpression == null)
                    throw new ArgumentNullException(nameof(filterExpression), "Parametr wyrażenia filtrującego jest pusty (null).");

                var addressEntity = DbContext.Addresses.FirstOrDefault(filterExpression);
                return Mapper.Map<AddressVm>(addressEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public IEnumerable<AddressVm> GetAddresses(Expression<Func<Address, bool>>? filterExpression = null)
        {
            try
            {
                var addressesQuery = DbContext.Addresses.AsQueryable();
                if (filterExpression != null)
                    addressesQuery = addressesQuery.Where(filterExpression);

                return Mapper.Map<IEnumerable<AddressVm>>(addressesQuery);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public void DeleteAddress(int id)
        {
            try
            {
                var addressEntity = DbContext.Addresses.Find(id)
                    ?? throw new KeyNotFoundException($"Nie znaleziono adresu o Id {id}.");

                var referencingCustomers = DbContext.Customers
                    .Where(c => c.BillingAddressId == id || c.ShippingAddressId == id)
                    .ToList();
                foreach (var customer in referencingCustomers)
                {
                    if (customer.BillingAddressId == id)
                        customer.BillingAddressId = null;
                    if (customer.ShippingAddressId == id)
                        customer.ShippingAddressId = null;
                }

                DbContext.Addresses.Remove(addressEntity);
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public IEnumerable<AddressVm> GetCustomerAddresses(int customerId)
        {
            return GetAddresses(a => a.CustomerId == customerId);
        }

        public IEnumerable<AddressVm> GetStoreAddresses(int stationaryStoreId)
        {
            return GetAddresses(a => a.StationaryStoreId == stationaryStoreId);
        }
    }
}
