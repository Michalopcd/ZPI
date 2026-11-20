using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Web.Data;

namespace WebStore.Tests.IntegrationTests
{
    public class TestUserSeederIntegrationTests
    {
        [Fact]
        public async Task SeedCreatesTestUserInDevelopmentTest()
        {
            using var factory = new WebStoreApiFactory();
            using var client = factory.CreateAnonymousClient();

            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var testUser = await userManager.FindByEmailAsync(TestUserSeeder.TestUserEmail);
            Assert.NotNull(testUser);
            Assert.IsAssignableFrom<Customer>(testUser);
            Assert.Equal(testUser.Email, testUser.UserName);
            Assert.Empty(await userManager.GetRolesAsync(testUser));
            Assert.True(TestUserSeeder.TestUserEmail.Length >= 6);
            Assert.True(TestUserSeeder.TestUserPassword.Length >= 6);
        }

        [Fact]
        public async Task SeedIsNotExecutedOutsideDevelopmentTest()
        {
            using var factory = new WebStoreApiFactory { EnvironmentName = Environments.Production };
            using var client = factory.CreateAnonymousClient();

            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            Assert.Null(await userManager.FindByEmailAsync(TestUserSeeder.TestUserEmail));

            var response = await WebStoreApiFactory.LoginAsync(client, TestUserSeeder.TestUserEmail, TestUserSeeder.TestUserPassword);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SeedIsIdempotentTest()
        {
            using var factory = new WebStoreApiFactory();
            using var client = factory.CreateAnonymousClient();
            var logger = factory.Services.GetRequiredService<ILogger<TestUserSeederIntegrationTests>>();

            await factory.Services.SeedTestUserAsync(logger);
            await factory.Services.SeedTestUserAsync(logger);

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(1, dbContext.Users.Count(u => u.Email == TestUserSeeder.TestUserEmail));
            Assert.DoesNotContain(factory.LoggerProvider.Entries, e => e.Level >= LogLevel.Warning && e.Message.Contains("użytkownika testowego"));
        }

        [Fact]
        public async Task SeedFailureDoesNotStopApplicationTest()
        {
            using var factory = new WebStoreApiFactory { UseUnavailableDatabase = true };
            using var client = factory.CreateAnonymousClient();

            var response = await client.GetAsync("/swagger/v1/swagger.json");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(factory.LoggerProvider.Entries, e => e.Level == LogLevel.Warning
                && e.Message.Contains("użytkownika testowego")
                && e.Exception != null);
        }
    }
}
