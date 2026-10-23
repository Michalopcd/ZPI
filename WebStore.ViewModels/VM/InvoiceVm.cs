namespace WebStore.ViewModels.VM
{
    public class InvoiceVm
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = default!;
        public DateTime IssueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public IList<int> OrderIds { get; set; } = new List<int>();
    }
}
