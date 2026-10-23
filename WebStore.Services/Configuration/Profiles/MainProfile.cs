using AutoMapper;
using WebStore.Model.DataModels;
using WebStore.ViewModels.VM;

namespace WebStore.Services.Configuration.Profiles
{
    public class MainProfile : Profile
    {
        public MainProfile()
        {
            CreateMap<AddOrUpdateProductVm, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<Product, ProductVm>()
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.ProductStocks.Sum(ps => ps.Quantity)));

            CreateMap<AddOrUpdateAddressVm, Address>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<Address, AddressVm>();

            CreateMap<AddOrUpdateOrderVm, Order>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<Order, OrderVm>();
            CreateMap<OrderProduct, OrderProductVm>();

            CreateMap<AddOrUpdateInvoiceVm, Invoice>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<Invoice, InvoiceVm>()
                .ForMember(dest => dest.OrderIds, opt => opt.MapFrom(src => src.Orders.Select(o => o.Id)));

            CreateMap<AddOrUpdateStoreVm, StationaryStore>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<StationaryStore, StoreVm>()
                .ForMember(dest => dest.EmployeeIds, opt => opt.MapFrom(src => src.Employees.Select(e => e.Id)));
        }
    }
}
