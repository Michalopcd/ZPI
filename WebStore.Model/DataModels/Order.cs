namespace WebStore.Model.DataModels
{
    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; } = default!;
        public DateTime OrderDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public decimal TotalAmount => OrderProducts.Sum(orderProduct => orderProduct.Quantity * orderProduct.Product.Price);
        public long TrackingNumber { get; set; }
        public int? InvoiceId { get; set; }
        public virtual Invoice? Invoice { get; set; }
        public virtual IList<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();
    }
}
