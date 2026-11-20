using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebStore.Services.ConcreteServices;

namespace WebStore.Web.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public abstract class BaseApiController : Controller
{
    private const string InvalidValueMessage = "Niepoprawny format danych.";
    private const string NotFoundMessage = "Nie znaleziono żądanego zasobu.";
    private const string BadRequestMessage = "Nie można wykonać operacji.";

    protected readonly ILogger Logger;
    protected readonly IMapper Mapper;

    protected BaseApiController(ILogger logger, IMapper mapper)
    {
        Logger = logger;
        Mapper = mapper;
    }

    [NonAction]
    public override BadRequestObjectResult BadRequest(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrEmpty(error.ErrorMessage) ? InvalidValueMessage : error.ErrorMessage)
                    .ToArray());
        return base.BadRequest(errors);
    }

    protected void ValidateIdForCreate(int? id)
    {
        if (id > 0)
            ModelState.AddModelError("Id", "Żądanie POST tworzy nowy rekord - pole Id musi być puste lub równe 0.");
    }

    protected void ValidateIdForUpdate(int? id)
    {
        if (id is null or 0)
            ModelState.AddModelError("Id", "Żądanie PUT aktualizuje istniejący rekord - pole Id jest wymagane.");
    }

    protected IActionResult HandleException(Exception ex)
    {
        Logger.LogError(ex, ex.Message);
        return ex switch
        {
            KeyNotFoundException => NotFound(GetClientMessage(ex, NotFoundMessage)),
            ArgumentException or InvalidOperationException => BadRequest(GetClientMessage(ex, BadRequestMessage)),
            _ => StatusCode(500, "Error occurred")
        };
    }

    private static string GetClientMessage(Exception ex, string defaultMessage)
    {
        if (ex.TargetSite?.DeclaringType?.Assembly != typeof(BaseService).Assembly)
            return defaultMessage;

        return ex is ArgumentException { ParamName: { } paramName }
            ? ex.Message.Replace($" (Parameter '{paramName}')", string.Empty)
            : ex.Message;
    }
}
