using System.ComponentModel.DataAnnotations;

namespace WebStore.ViewModels.VM
{
    public class AddOrUpdateAddressVm
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Ulica jest wymagana.")]
        public string Street { get; set; } = default!;

        [Required(ErrorMessage = "Numer budynku jest wymagany.")]
        public string BuildingNumber { get; set; } = default!;

        public string? ApartmentNumber { get; set; }

        [Required(ErrorMessage = "Kod pocztowy jest wymagany.")]
        public string PostalCode { get; set; } = default!;

        [Required(ErrorMessage = "Miasto jest wymagane.")]
        public string City { get; set; } = default!;

        [Required(ErrorMessage = "Kraj jest wymagany.")]
        public string Country { get; set; } = default!;

        public int? CustomerId { get; set; }

        public int? StationaryStoreId { get; set; }
    }
}
