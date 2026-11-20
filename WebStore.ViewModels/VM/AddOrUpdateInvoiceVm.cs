using System.ComponentModel.DataAnnotations;

namespace WebStore.ViewModels.VM
{
    public class AddOrUpdateInvoiceVm
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Numer faktury jest wymagany.")]
        public string InvoiceNumber { get; set; } = default!;

        [Required(ErrorMessage = "Data wystawienia faktury jest wymagana.")]
        public DateTime? IssueDate { get; set; }

        [Required(ErrorMessage = "Kwota faktury jest wymagana.")]
        public decimal? TotalAmount { get; set; }
    }
}
