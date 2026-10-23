namespace WebStore.Model.DataModels
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = default!;
        public DateTime IssueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public virtual IList<Order> Orders { get; set; } = new List<Order>();
    }
}
