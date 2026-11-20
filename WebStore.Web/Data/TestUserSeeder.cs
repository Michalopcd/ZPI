using Microsoft.AspNetCore.Identity;
using WebStore.Model.DataModels;

namespace WebStore.Web.Data;

public static class TestUserSeeder
{
    public const string TestUserEmail = "test.user@webstore.pl";
    public const string TestUserPassword = "Test1234";

    public static async Task SeedTestUserAsync(this IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            if (await userManager.FindByEmailAsync(TestUserEmail) != null)
                return;

            var testUser = new Customer
            {
                FirstName = "Użytkownik",
                LastName = "Testowy",
                UserName = TestUserEmail,
                Email = TestUserEmail,
                RegistrationDate = DateTime.Now
            };
            var result = await userManager.CreateAsync(testUser, TestUserPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Nie udało się utworzyć użytkownika testowego {TestUserEmail}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Nie udało się utworzyć użytkownika testowego: {Message}", ex.Message);
        }
    }
}
