using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using WebStore.DAL.EF;

namespace WebStore.Tests.IntegrationTests
{
    public abstract class BaseApiIntegrationTests : IClassFixture<WebStoreApiFactory>
    {
        protected const int NotExistingId = 999999;

        protected readonly WebStoreApiFactory Factory;

        protected BaseApiIntegrationTests(WebStoreApiFactory factory)
        {
            Factory = factory;
        }

        protected async Task<HttpStatusCode> SendAnonymousRequestAsync(string method, string url)
        {
            using var client = Factory.CreateAnonymousClient();
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        protected static JsonContent CreateJsonContentWithout<T>(T model, string propertyName)
        {
            var json = JsonSerializer.SerializeToNode(model, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
            if (!json.Remove(propertyName))
                throw new ArgumentException($"Model nie zawiera właściwości {propertyName}.", nameof(propertyName));

            return JsonContent.Create(json);
        }

        protected T QueryDatabase<T>(Func<ApplicationDbContext, T> query)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return query(dbContext);
        }
    }
}
