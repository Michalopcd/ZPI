namespace WebStore.ViewModels.VM
{
    public class StoreVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public IList<AddressVm> Addresses { get; set; } = new List<AddressVm>();
        public IList<int> EmployeeIds { get; set; } = new List<int>();
    }
}
