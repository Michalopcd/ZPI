using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Web.Controllers;

[Authorize]
public class InvoiceApiController : BaseApiController
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceApiController(ILogger logger, IMapper mapper,
        IInvoiceService invoiceService)
        : base(logger, mapper)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            return Ok(_invoiceService.GetInvoices());
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
            var invoice = _invoiceService.GetInvoice(i => i.Id == id);
            if (invoice == null)
                return NotFound($"Nie znaleziono faktury o Id {id}.");

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut]
    public IActionResult Put([FromBody] AddOrUpdateInvoiceVm addOrUpdateInvoiceVm)
    {
        return PostOrPutHelper(addOrUpdateInvoiceVm, ValidateIdForUpdate);
    }

    [HttpPost]
    public IActionResult Post([FromBody] AddOrUpdateInvoiceVm addOrUpdateInvoiceVm)
    {
        return PostOrPutHelper(addOrUpdateInvoiceVm, ValidateIdForCreate);
    }

    [HttpDelete("{id:int:min(1)}")]
    public IActionResult Delete(int id)
    {
        try
        {
            _invoiceService.DeleteInvoice(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("Issue")]
    public IActionResult IssueInvoice([FromBody] IEnumerable<int> orderIds)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(_invoiceService.IssueInvoice(orderIds));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private IActionResult PostOrPutHelper(AddOrUpdateInvoiceVm addOrUpdateInvoiceVm, Action<int?> validateId)
    {
        try
        {
            if (ModelState.IsValid)
                validateId(addOrUpdateInvoiceVm.Id);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(_invoiceService.AddOrUpdateInvoice(addOrUpdateInvoiceVm));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
