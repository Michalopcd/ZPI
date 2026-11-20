using System.ComponentModel.DataAnnotations;

namespace WebStore.ViewModels.VM
{
    public class AddOrUpdateOrderVm
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Klient zamówienia jest wymagany.")]
        public int? CustomerId { get; set; }

        [Required(ErrorMessage = "Data zamówienia jest wymagana.")]
        public DateTime? OrderDate { get; set; }

        [Required(ErrorMessage = "Data dostawy jest wymagana.")]
        public DateTime? DeliveryDate { get; set; }

        [Required(ErrorMessage = "Numer przesyłki jest wymagany.")]
        public long? TrackingNumber { get; set; }
    }
}
