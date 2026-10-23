namespace WebStore.ViewModels.VM
{
    public class AddressVm
    {
        public int Id { get; set; }
        public string Street { get; set; } = default!;
        public string BuildingNumber { get; set; } = default!;
        public string? ApartmentNumber { get; set; }
        public string PostalCode { get; set; } = default!;
        public string City { get; set; } = default!;
        public string Country { get; set; } = default!;
        public int? CustomerId { get; set; }
        public int? StationaryStoreId { get; set; }
    }
}
