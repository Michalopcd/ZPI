namespace WebStore.ViewModels.VM
{
    public class OrderProductVm
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; }
    }
}
