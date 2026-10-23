namespace WebStore.Model.DataModels
{
    public class StationaryStoreEmployee : User
    {
        public string Position { get; set; } = default!;
        public int StationaryStoreId { get; set; }
        public virtual StationaryStore StationaryStore { get; set; } = default!;
    }
}
