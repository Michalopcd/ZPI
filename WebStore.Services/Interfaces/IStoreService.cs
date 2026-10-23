using System.Linq.Expressions;
using WebStore.Model.DataModels;
using WebStore.ViewModels.VM;

namespace WebStore.Services.Interfaces
{
    public interface IStoreService
    {
        StoreVm AddOrUpdateStore(AddOrUpdateStoreVm addOrUpdateStoreVm);
        StoreVm GetStore(Expression<Func<StationaryStore, bool>> filterExpression);
        IEnumerable<StoreVm> GetStores(Expression<Func<StationaryStore, bool>>? filterExpression = null);
        void DeleteStore(int id);
        StoreVm AddEmployeeToStore(int stationaryStoreId, int employeeId);
        StoreVm AddAddressToStore(int stationaryStoreId, AddOrUpdateAddressVm addressVm);
    }
}
