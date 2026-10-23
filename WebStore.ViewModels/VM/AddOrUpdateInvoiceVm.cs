using System.ComponentModel.DataAnnotations;

namespace WebStore.ViewModels.VM
{
    public class AddOrUpdateInvoiceVm
    {
        public int? Id { get; set; }

        [Required]
        public string InvoiceNumber { get; set; } = default!;

        [Required]
        public DateTime IssueDate { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }
    }
}
