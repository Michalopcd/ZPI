using System.Linq.Expressions;
using WebStore.Model.DataModels;
using WebStore.ViewModels.VM;

namespace WebStore.Services.Interfaces
{
    public interface IAddressService
    {
        AddressVm AddOrUpdateAddress(AddOrUpdateAddressVm addOrUpdateAddressVm);
        AddressVm GetAddress(Expression<Func<Address, bool>> filterExpression);
        IEnumerable<AddressVm> GetAddresses(Expression<Func<Address, bool>>? filterExpression = null);
        void DeleteAddress(int id);
        IEnumerable<AddressVm> GetCustomerAddresses(int customerId);
        IEnumerable<AddressVm> GetStoreAddresses(int stationaryStoreId);
    }
}
