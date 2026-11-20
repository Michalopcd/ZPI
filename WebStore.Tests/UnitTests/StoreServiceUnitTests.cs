using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Tests.UnitTests
{
    public class StoreServiceUnitTests : BaseUnitTests
    {
        private readonly IStoreService _storeService;

        public StoreServiceUnitTests(ApplicationDbContext dbContext, IStoreService storeService)
            : base(dbContext)
        {
            _storeService = storeService;
        }

        [Fact]
        public void GetStoreTest()
        {
            var store = _storeService.GetStore(s => s.Id == 1);
            Assert.NotNull(store);
            Assert.Equal("WebStore Gliwice", store.Name);
            Assert.Contains(store.Addresses, a => a.Id == 3);
            Assert.Contains(3, store.EmployeeIds);
        }

        [Fact]
        public void GetStoreWithNullFilterThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => _storeService.GetStore(null!));
        }

        [Fact]
        public void GetAllStoresTest()
        {
            var stores = _storeService.GetStores();
            Assert.NotEmpty(stores);
            Assert.Equal(DbContext.StationaryStores.Count(), stores.Count());
        }

        [Fact]
        public void GetFilteredStoresTest()
        {
            var stores = _storeService.GetStores(s => s.Name == "WebStore Gliwice").ToList();
            Assert.Single(stores);
            Assert.Equal(1, stores[0].Id);
        }

        [Fact]
        public void AddNewStoreTest()
        {
            var createdStore = _storeService.AddOrUpdateStore(new AddOrUpdateStoreVm() { Name = "WebStore Katowice" });
            Assert.NotNull(createdStore);
            Assert.True(createdStore.Id > 0);
            Assert.Equal("WebStore Katowice", createdStore.Name);
            Assert.NotNull(DbContext.StationaryStores.Find(createdStore.Id));
        }

        [Fact]
        public void UpdateStoreTest()
        {
            var createdStore = _storeService.AddOrUpdateStore(new AddOrUpdateStoreVm() { Name = "WebStore Bytom" });

            var updatedStore = _storeService.AddOrUpdateStore(new AddOrUpdateStoreVm() { Id = createdStore.Id, Name = "WebStore Zabrze" });

            Assert.Equal(createdStore.Id, updatedStore.Id);
            Assert.Equal("WebStore Zabrze", updatedStore.Name);
        }

        [Fact]
        public void AddOrUpdateStoreWithNullThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => _storeService.AddOrUpdateStore(null!));
        }

        [Fact]
        public void DeleteStoreTest()
        {
            var createdStore = _storeService.AddOrUpdateStore(new AddOrUpdateStoreVm() { Name = "WebStore Rybnik" });
            var storeWithAddress = _storeService.AddAddressToStore(createdStore.Id, CreateAddressVm());
            var addressId = Assert.Single(storeWithAddress.Addresses).Id;

            _storeService.DeleteStore(createdStore.Id);

            Assert.Null(_storeService.GetStore(s => s.Id == createdStore.Id));
            Assert.Null(DbContext.Addresses.Find(addressId));
        }

        [Fact]
        public void DeleteStoreWithEmployeesThrowsTest()
        {
            Assert.Throws<InvalidOperationException>(() => _storeService.DeleteStore(1));
        }

        [Fact]
        public void DeleteNotExistingStoreThrowsTest()
        {
            Assert.Throws<KeyNotFoundException>(() => _storeService.DeleteStore(int.MaxValue));
        }

        [Fact]
        public void AddEmployeeToStoreTest()
        {
            var employee = new StationaryStoreEmployee()
            {
                FirstName = "Piotr",
                LastName = "Zieliński",
                UserName = "emp2@eg.eg",
                Email = "emp2@eg.eg",
                RegistrationDate = new DateTime(2024, 1, 2),
                Position = "Kierownik",
                StationaryStoreId = 1
            };
            DbContext.StationaryStoreEmployees.Add(employee);
            DbContext.SaveChanges();
            var createdStore = _storeService.AddOrUpdateStore(new AddOrUpdateStoreVm() { Name = "WebStore Tychy" });

            var storeWithEmployee = _storeService.AddEmployeeToStore(createdStore.Id, employee.Id);

            Assert.Contains(employee.Id, storeWithEmployee.EmployeeIds);
            Assert.Equal(createdStore.Id, DbContext.StationaryStoreEmployees.Find(employee.Id)!.StationaryStoreId);
            Assert.DoesNotContain(employee.Id, _storeService.GetStore(s => s.Id == 1).EmployeeIds);
        }

        [Fact]
        public void AddAddressToStoreTest()
        {
            var createdStore = _storeService.AddOrUpdateStore(new AddOrUpdateStoreVm() { Name = "WebStore Opole" });

            var storeWithAddress = _storeService.AddAddressToStore(createdStore.Id, CreateAddressVm());

            var address = Assert.Single(storeWithAddress.Addresses);
            Assert.True(address.Id > 0);
            Assert.Equal(createdStore.Id, address.StationaryStoreId);
            Assert.Equal("Rynek", address.Street);
        }

        [Fact]
        public void AddAddressToStoreWithNullThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => _storeService.AddAddressToStore(1, null!));
        }

        [Fact]
        public void AddAddressWithNotExistingCustomerToStoreThrowsTest()
        {
            var createdStore = _storeService.AddOrUpdateStore(new AddOrUpdateStoreVm() { Name = "WebStore Rybnik" });
            var addressVm = CreateAddressVm();
            addressVm.CustomerId = int.MaxValue;

            var exception = Assert.Throws<ArgumentException>(() => _storeService.AddAddressToStore(createdStore.Id, addressVm));

            Assert.Equal($"Nie znaleziono klienta o Id {int.MaxValue}.", exception.Message);
            DbContext.ChangeTracker.Clear();
            Assert.False(DbContext.Addresses.Any(a => a.StationaryStoreId == createdStore.Id));
        }

        private static AddOrUpdateAddressVm CreateAddressVm()
        {
            return new AddOrUpdateAddressVm()
            {
                Street = "Rynek",
                BuildingNumber = "10",
                PostalCode = "45-001",
                City = "Opole",
                Country = "Polska"
            };
        }
    }
}
