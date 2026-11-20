using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Web.Controllers;

[Authorize]
public class OrderApiController : BaseApiController
{
    private readonly IOrderService _orderService;

    public OrderApiController(ILogger logger, IMapper mapper,
        IOrderService orderService)
        : base(logger, mapper)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            return Ok(_orderService.GetOrders());
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
            var order = _orderService.GetOrder(o => o.Id == id);
            if (order == null)
                return NotFound($"Nie znaleziono zamówienia o Id {id}.");

            return Ok(order);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut]
    public IActionResult Put([FromBody] AddOrUpdateOrderVm addOrUpdateOrderVm)
    {
        return PostOrPutHelper(addOrUpdateOrderVm, ValidateIdForUpdate);
    }

    [HttpPost]
    public IActionResult Post([FromBody] AddOrUpdateOrderVm addOrUpdateOrderVm)
    {
        return PostOrPutHelper(addOrUpdateOrderVm, ValidateIdForCreate);
    }

    [HttpDelete("{id:int:min(1)}")]
    public IActionResult Delete(int id)
    {
        try
        {
            _orderService.DeleteOrder(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{orderId:int:min(1)}/Products/{productId:int:min(1)}")]
    public IActionResult AddProductToOrder(int orderId, int productId, [FromQuery] int quantity)
    {
        try
        {
            return Ok(_orderService.AddProductToOrder(orderId, productId, quantity));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpGet("Customer/{customerId:int:min(1)}")]
    public IActionResult GetCustomerOrders(int customerId)
    {
        try
        {
            return Ok(_orderService.GetCustomerOrders(customerId));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private IActionResult PostOrPutHelper(AddOrUpdateOrderVm addOrUpdateOrderVm, Action<int?> validateId)
    {
        try
        {
            if (ModelState.IsValid)
                validateId(addOrUpdateOrderVm.Id);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(_orderService.AddOrUpdateOrder(addOrUpdateOrderVm));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
