using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Web.Controllers;

[Authorize]
public class ProductApiController : BaseApiController
{
    private readonly IProductService _productService;

    public ProductApiController(ILogger logger, IMapper mapper,
        IProductService productService)
        : base(logger, mapper)
    {
        _productService = productService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            var products = _productService.GetProducts();
            return Ok(products);
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
            var product = _productService.GetProduct(p => p.Id == id);
            if (product == null)
                return NotFound($"Nie znaleziono produktu o Id {id}.");

            return Ok(product);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut]
    public IActionResult Put([FromBody] AddOrUpdateProductVm addOrUpdateProductVm)
    {
        return PostOrPutHelper(addOrUpdateProductVm, ValidateIdForUpdate);
    }

    [HttpPost]
    public IActionResult Post([FromBody] AddOrUpdateProductVm addOrUpdateProductVm)
    {
        return PostOrPutHelper(addOrUpdateProductVm, ValidateIdForCreate);
    }

    [HttpDelete("{id:int:min(1)}")]
    public IActionResult Delete(int id)
    {
        try
        {
            _productService.DeleteProduct(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private IActionResult PostOrPutHelper(AddOrUpdateProductVm addOrUpdateProductVm, Action<int?> validateId)
    {
        try
        {
            if (ModelState.IsValid)
                validateId(addOrUpdateProductVm.Id);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(_productService.AddOrUpdateProduct(addOrUpdateProductVm));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
