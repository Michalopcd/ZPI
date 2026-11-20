using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Web.Controllers;

[Authorize]
public class StoreApiController : BaseApiController
{
    private readonly IStoreService _storeService;

    public StoreApiController(ILogger logger, IMapper mapper,
        IStoreService storeService)
        : base(logger, mapper)
    {
        _storeService = storeService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            return Ok(_storeService.GetStores());
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
            var store = _storeService.GetStore(s => s.Id == id);
            if (store == null)
                return NotFound($"Nie znaleziono sklepu o Id {id}.");

            return Ok(store);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut]
    public IActionResult Put([FromBody] AddOrUpdateStoreVm addOrUpdateStoreVm)
    {
        return PostOrPutHelper(addOrUpdateStoreVm, ValidateIdForUpdate);
    }

    [HttpPost]
    public IActionResult Post([FromBody] AddOrUpdateStoreVm addOrUpdateStoreVm)
    {
        return PostOrPutHelper(addOrUpdateStoreVm, ValidateIdForCreate);
    }

    [HttpDelete("{id:int:min(1)}")]
    public IActionResult Delete(int id)
    {
        try
        {
            _storeService.DeleteStore(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{stationaryStoreId:int:min(1)}/Employees/{employeeId:int:min(1)}")]
    public IActionResult AddEmployeeToStore(int stationaryStoreId, int employeeId)
    {
        try
        {
            return Ok(_storeService.AddEmployeeToStore(stationaryStoreId, employeeId));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{stationaryStoreId:int:min(1)}/Addresses")]
    public IActionResult AddAddressToStore(int stationaryStoreId, [FromBody] AddOrUpdateAddressVm addressVm)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(_storeService.AddAddressToStore(stationaryStoreId, addressVm));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private IActionResult PostOrPutHelper(AddOrUpdateStoreVm addOrUpdateStoreVm, Action<int?> validateId)
    {
        try
        {
            if (ModelState.IsValid)
                validateId(addOrUpdateStoreVm.Id);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(_storeService.AddOrUpdateStore(addOrUpdateStoreVm));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
