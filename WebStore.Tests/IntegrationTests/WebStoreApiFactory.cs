using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Web.Data;

namespace WebStore.Tests.IntegrationTests
{
    public class WebStoreApiFactory : WebApplicationFactory<Program>
    {
        public const string LoginUrl = "/api/AccountApi/Login";
        public const string UserWithRoleLogin = "manager@eg.eg";
        public const string UserWithRolePassword = "User1234";
        public const string UserWithRoleRole = "Manager";
        public const string UserWithoutEmailLogin = "bez.adresu.email";
        public const string UserWithoutEmailPassword = "User1234";
        public const string UnavailableDatabaseConnectionString =
            "Server=tcp:127.0.0.1,1;Database=Michal_OpasAppDb;Trusted_Connection=True;Connect Timeout=2";

        private readonly string _databaseName = $"WebStoreApi_{Guid.NewGuid()}";
        private readonly InMemoryDatabaseRoot _databaseRoot = new();

        public string EnvironmentName { get; init; } = Environments.Development;

        public bool UseUnavailableDatabase { get; init; }

        public TestLoggerProvider LoggerProvider { get; } = new();

        public HttpClient CreateAnonymousClient()
        {
            return CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        }

        public async Task<HttpClient> CreateAuthorizedClientAsync()
        {
            var client = CreateAnonymousClient();
            var token = await GetTokenAsync(client, TestUserSeeder.TestUserEmail, TestUserSeeder.TestUserPassword);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string login, string password)
        {
            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Login"] = login,
                ["Password"] = password
            });
            return client.PostAsync(LoginUrl, form);
        }

        public static async Task<string> GetTokenAsync(HttpClient client, string login, string password)
        {
            var response = await LoginAsync(client, login, password);
            response.EnsureSuccessStatusCode();
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return loginResponse!.AccessToken;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(EnvironmentName);
            builder.ConfigureLogging(logging => logging.AddProvider(LoggerProvider));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                if (UseUnavailableDatabase)
                {
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseLazyLoadingProxies()
                            .UseSqlServer(UnavailableDatabaseConnectionString));
                    return;
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseLazyLoadingProxies()
                        .UseInMemoryDatabase(_databaseName, _databaseRoot));
                services.SeedData();
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            if (!UseUnavailableDatabase)
                SeedApiUsersAsync(host.Services).GetAwaiter().GetResult();

            return host;
        }

        private static async Task SeedApiUsersAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            await EnsureSucceededAsync(roleManager.CreateAsync(new IdentityRole<int>(UserWithRoleRole)));

            var userWithRole = new Customer
            {
                FirstName = "Marek",
                LastName = "Kierownik",
                UserName = UserWithRoleLogin,
                Email = UserWithRoleLogin,
                RegistrationDate = new DateTime(2020, 1, 1)
            };
            await EnsureSucceededAsync(userManager.CreateAsync(userWithRole, UserWithRolePassword));
            await EnsureSucceededAsync(userManager.AddToRoleAsync(userWithRole, UserWithRoleRole));

            var userWithoutEmail = new Customer
            {
                FirstName = "Ewa",
                LastName = "Bezadresowa",
                UserName = UserWithoutEmailLogin,
                RegistrationDate = new DateTime(2020, 1, 1)
            };
            await EnsureSucceededAsync(userManager.CreateAsync(userWithoutEmail, UserWithoutEmailPassword));
        }

        private static async Task EnsureSucceededAsync(Task<IdentityResult> operation)
        {
            var result = await operation;
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        public class LoginResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = default!;

            [JsonPropertyName("expires_in")]
            public string ExpiresIn { get; set; } = default!;
        }
    }
}
