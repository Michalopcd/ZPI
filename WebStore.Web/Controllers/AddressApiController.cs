using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Web.Controllers;

[Authorize]
public class AddressApiController : BaseApiController
{
    private readonly IAddressService _addressService;

    public AddressApiController(ILogger logger, IMapper mapper,
        IAddressService addressService)
        : base(logger, mapper)
    {
        _addressService = addressService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            return Ok(_addressService.GetAddresses());
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("{id:int:min(1)}")]
    public IActionResult Get(int id)
    {
        try
        {
            var address = _addressService.GetAddress(a => a.Id == id);
            if (address == null)
                return NotFound($"Nie znaleziono adresu o Id {id}.");

            return Ok(address);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut]
    public IActionResult Put([FromBody] AddOrUpdateAddressVm addOrUpdateAddressVm)
    {
        return PostOrPutHelper(addOrUpdateAddressVm, ValidateIdForUpdate);
    }

    [HttpPost]
    public IActionResult Post([FromBody] AddOrUpdateAddressVm addOrUpdateAddressVm)
    {
        return PostOrPutHelper(addOrUpdateAddressVm, ValidateIdForCreate);
    }

    [HttpDelete("{id:int:min(1)}")]
    public IActionResult Delete(int id)
    {
        try
        {
            _addressService.DeleteAddress(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("Customer/{customerId:int:min(1)}")]
    public IActionResult GetCustomerAddresses(int customerId)
    {
        try
        {
            return Ok(_addressService.GetCustomerAddresses(customerId));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("Store/{stationaryStoreId:int:min(1)}")]
    public IActionResult GetStoreAddresses(int stationaryStoreId)
    {
        try
        {
            return Ok(_addressService.GetStoreAddresses(stationaryStoreId));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private IActionResult PostOrPutHelper(AddOrUpdateAddressVm addOrUpdateAddressVm, Action<int?> validateId)
    {
        try
        {
            if (ModelState.IsValid)
                validateId(addOrUpdateAddressVm.Id);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(_addressService.AddOrUpdateAddress(addOrUpdateAddressVm));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
