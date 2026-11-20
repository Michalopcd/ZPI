using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Services.ConcreteServices
{
    public class StoreService : BaseService, IStoreService
    {
        public StoreService(ApplicationDbContext dbContext, IMapper mapper, ILogger logger)
            : base(dbContext, mapper, logger)
        {
        }

        public StoreVm AddOrUpdateStore(AddOrUpdateStoreVm addOrUpdateStoreVm)
        {
            try
            {
                if (addOrUpdateStoreVm == null)
                    throw new ArgumentNullException(nameof(addOrUpdateStoreVm), "Parametr view modelu jest pusty (null).");

                StationaryStore storeEntity;
                if (addOrUpdateStoreVm.Id is null or 0)
                {
                    storeEntity = Mapper.Map<StationaryStore>(addOrUpdateStoreVm);
                    DbContext.StationaryStores.Add(storeEntity);
                }
                else
                {
                    storeEntity = DbContext.StationaryStores.Find(addOrUpdateStoreVm.Id.Value)
                        ?? throw new KeyNotFoundException($"Nie znaleziono sklepu o Id {addOrUpdateStoreVm.Id.Value}.");
                    Mapper.Map(addOrUpdateStoreVm, storeEntity);
                }

                DbContext.SaveChanges();
                return Mapper.Map<StoreVm>(storeEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public StoreVm GetStore(Expression<Func<StationaryStore, bool>> filterExpression)
        {
            try
            {
                if (filterExpression == null)
                    throw new ArgumentNullException(nameof(filterExpression), "Parametr wyrażenia filtrującego jest pusty (null).");

                var storeEntity = DbContext.StationaryStores.FirstOrDefault(filterExpression);
                return Mapper.Map<StoreVm>(storeEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public IEnumerable<StoreVm> GetStores(Expression<Func<StationaryStore, bool>>? filterExpression = null)
        {
            try
            {
                var storesQuery = DbContext.StationaryStores.AsQueryable();
                if (filterExpression != null)
                    storesQuery = storesQuery.Where(filterExpression);

                return Mapper.Map<IEnumerable<StoreVm>>(storesQuery);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public void DeleteStore(int id)
        {
            try
            {
                var storeEntity = DbContext.StationaryStores.Find(id)
                    ?? throw new KeyNotFoundException($"Nie znaleziono sklepu o Id {id}.");

                if (storeEntity.Employees.Count > 0)
                    throw new InvalidOperationException($"Nie można usunąć sklepu o Id {id}, ponieważ ma przypisanych pracowników.");

                foreach (var address in storeEntity.Addresses.ToList())
                {
                    if (address.CustomerId == null)
                        DbContext.Addresses.Remove(address);
                    else
                        address.StationaryStoreId = null;
                }

                DbContext.StationaryStores.Remove(storeEntity);
                DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public StoreVm AddEmployeeToStore(int stationaryStoreId, int employeeId)
        {
            try
            {
                var storeEntity = DbContext.StationaryStores.Find(stationaryStoreId)
                    ?? throw new KeyNotFoundException($"Nie znaleziono sklepu o Id {stationaryStoreId}.");
                var employeeEntity = DbContext.StationaryStoreEmployees.Find(employeeId)
                    ?? throw new KeyNotFoundException($"Nie znaleziono pracownika o Id {employeeId}.");

                employeeEntity.StationaryStore = storeEntity;

                DbContext.SaveChanges();
                return Mapper.Map<StoreVm>(storeEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public StoreVm AddAddressToStore(int stationaryStoreId, AddOrUpdateAddressVm addressVm)
        {
            try
            {
                if (addressVm == null)
                    throw new ArgumentNullException(nameof(addressVm), "Parametr view modelu jest pusty (null).");

                var storeEntity = DbContext.StationaryStores.Find(stationaryStoreId)
                    ?? throw new KeyNotFoundException($"Nie znaleziono sklepu o Id {stationaryStoreId}.");
                if (addressVm.CustomerId != null && !DbContext.Customers.Any(c => c.Id == addressVm.CustomerId))
                    throw new ArgumentException($"Nie znaleziono klienta o Id {addressVm.CustomerId}.");

                var addressEntity = Mapper.Map<Address>(addressVm);
                addressEntity.StationaryStoreId = storeEntity.Id;
                storeEntity.Addresses.Add(addressEntity);

                DbContext.SaveChanges();
                return Mapper.Map<StoreVm>(storeEntity);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
