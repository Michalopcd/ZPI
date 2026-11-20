using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using WebStore.Services.ConcreteServices;
using WebStore.Web.Controllers;

namespace WebStore.Tests.UnitTests
{
    public class BaseApiControllerUnitTests
    {
        private readonly TestApiController _controller = new();

        [Fact]
        public void TechnicalExceptionMessageIsNotReturnedTest()
        {
            var exception = Catch(() => new List<int>().First());

            var result = Assert.IsType<BadRequestObjectResult>(_controller.Handle(exception));

            Assert.Equal("Nie można wykonać operacji.", result.Value);
        }

        [Fact]
        public void TechnicalKeyNotFoundExceptionMessageIsNotReturnedTest()
        {
            var exception = Catch(() => new Dictionary<int, int>()[1]);

            var result = Assert.IsType<NotFoundObjectResult>(_controller.Handle(exception));

            Assert.Equal("Nie znaleziono żądanego zasobu.", result.Value);
        }

        [Fact]
        public void ServiceExceptionMessageIsReturnedWithoutParameterNameTest()
        {
            var invoiceService = new InvoiceService(null!, null!, NullLogger.Instance);
            var exception = Catch(() => invoiceService.IssueInvoice(Array.Empty<int>()));

            var result = Assert.IsType<BadRequestObjectResult>(_controller.Handle(exception));

            Assert.Equal("Faktura musi obejmować co najmniej jedno zamówienie.", result.Value);
        }

        [Fact]
        public void UnexpectedExceptionReturnsInternalServerErrorWithoutDetailsTest()
        {
            var exception = Catch(() => throw new NullReferenceException("Szczegóły techniczne."));

            var result = Assert.IsType<ObjectResult>(_controller.Handle(exception));

            Assert.Equal(500, result.StatusCode);
            Assert.Equal("Error occurred", result.Value);
        }

        private static Exception Catch(Func<object> action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Oczekiwano wyjątku.");
        }

        private class TestApiController : BaseApiController
        {
            public TestApiController()
                : base(NullLogger.Instance, null!)
            {
            }

            public IActionResult Handle(Exception exception)
            {
                return HandleException(exception);
            }
        }
    }
}
