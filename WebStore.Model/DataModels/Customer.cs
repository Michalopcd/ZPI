namespace WebStore.Model.DataModels
{
    public class Customer : User
    {
        public virtual IList<Order> Orders { get; set; } = new List<Order>();
        public virtual IList<Address> Addresses { get; set; } = new List<Address>();
        public int? BillingAddressId { get; set; }
        public virtual Address? BillingAddress { get; set; }
        public int? ShippingAddressId { get; set; }
        public virtual Address? ShippingAddress { get; set; }
    }
}
