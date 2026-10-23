namespace WebStore.Model.DataModels
{
    public class StationaryStore
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public virtual IList<Address> Addresses { get; set; } = new List<Address>();
        public virtual IList<StationaryStoreEmployee> Employees { get; set; } = new List<StationaryStoreEmployee>();
    }
}
