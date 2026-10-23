using System.ComponentModel.DataAnnotations;

namespace WebStore.ViewModels.VM
{
    public class AddOrUpdateAddressVm
    {
        public int? Id { get; set; }

        [Required]
        public string Street { get; set; } = default!;

        [Required]
        public string BuildingNumber { get; set; } = default!;

        public string? ApartmentNumber { get; set; }

        [Required]
        public string PostalCode { get; set; } = default!;

        [Required]
        public string City { get; set; } = default!;

        [Required]
        public string Country { get; set; } = default!;

        public int? CustomerId { get; set; }

        public int? StationaryStoreId { get; set; }
    }
}
