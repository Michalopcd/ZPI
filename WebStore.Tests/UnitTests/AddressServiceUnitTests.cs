using WebStore.DAL.EF;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Tests.UnitTests
{
    public class AddressServiceUnitTests : BaseUnitTests
    {
        private readonly IAddressService _addressService;

        public AddressServiceUnitTests(ApplicationDbContext dbContext, IAddressService addressService)
            : base(dbContext)
        {
            _addressService = addressService;
        }

        [Fact]
        public void GetAddressTest()
        {
            var address = _addressService.GetAddress(a => a.Id == 1);
            Assert.NotNull(address);
            Assert.Equal("Akademicka", address.Street);
            Assert.Equal(2, address.CustomerId);
        }

        [Fact]
        public void GetAddressWithNullFilterThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => _addressService.GetAddress(null!));
        }

        [Fact]
        public void GetAllAddressesTest()
        {
            var addresses = _addressService.GetAddresses();
            Assert.NotEmpty(addresses);
            Assert.Equal(DbContext.Addresses.Count(), addresses.Count());
        }

        [Fact]
        public void GetFilteredAddressesTest()
        {
            var addresses = _addressService.GetAddresses(a => a.City == "Katowice").ToList();
            Assert.NotEmpty(addresses);
            Assert.All(addresses, a => Assert.Equal("Katowice", a.City));
        }

        [Fact]
        public void AddNewAddressTest()
        {
            var createdAddress = _addressService.AddOrUpdateAddress(CreateAddressVm("Nowa"));
            Assert.NotNull(createdAddress);
            Assert.True(createdAddress.Id > 0);
            Assert.Equal("Nowa", createdAddress.Street);
            Assert.Equal(2, createdAddress.CustomerId);
            Assert.NotNull(DbContext.Addresses.Find(createdAddress.Id));
        }

        [Fact]
        public void UpdateAddressTest()
        {
            var createdAddress = _addressService.AddOrUpdateAddress(CreateAddressVm("Do zmiany"));
            var updateAddressVm = CreateAddressVm("Zmieniona");
            updateAddressVm.Id = createdAddress.Id;
            updateAddressVm.City = "Zabrze";

            var updatedAddress = _addressService.AddOrUpdateAddress(updateAddressVm);
            Assert.Equal(createdAddress.Id, updatedAddress.Id);
            Assert.Equal("Zmieniona", updatedAddress.Street);
            Assert.Equal("Zabrze", updatedAddress.City);
        }

        [Fact]
        public void AddOrUpdateAddressWithNullThrowsTest()
        {
            Assert.Throws<ArgumentNullException>(() => _addressService.AddOrUpdateAddress(null!));
        }

        [Fact]
        public void DeleteAddressTest()
        {
            var createdAddress = _addressService.AddOrUpdateAddress(CreateAddressVm("Do usunięcia"));

            _addressService.DeleteAddress(createdAddress.Id);

            Assert.Null(_addressService.GetAddress(a => a.Id == createdAddress.Id));
        }

        [Fact]
        public void DeleteNotExistingAddressThrowsTest()
        {
            Assert.Throws<KeyNotFoundException>(() => _addressService.DeleteAddress(int.MaxValue));
        }

        [Fact]
        public void GetCustomerAddressesTest()
        {
            var addresses = _addressService.GetCustomerAddresses(2).ToList();
            Assert.True(addresses.Count >= 2);
            Assert.All(addresses, a => Assert.Equal(2, a.CustomerId));
            Assert.Contains(addresses, a => a.Id == 1);
            Assert.Contains(addresses, a => a.Id == 2);
        }

        [Fact]
        public void GetStoreAddressesTest()
        {
            var addresses = _addressService.GetStoreAddresses(1).ToList();
            Assert.NotEmpty(addresses);
            Assert.All(addresses, a => Assert.Equal(1, a.StationaryStoreId));
            Assert.Contains(addresses, a => a.Id == 3);
        }

        private static AddOrUpdateAddressVm CreateAddressVm(string street)
        {
            return new AddOrUpdateAddressVm()
            {
                Street = street,
                BuildingNumber = "7",
                ApartmentNumber = "3",
                PostalCode = "44-100",
                City = "Gliwice",
                Country = "Polska",
                CustomerId = 2
            };
        }
    }
}
