using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebStore.ViewModels.VM;
using WebStore.Web.Data;

namespace WebStore.Tests.IntegrationTests
{
    public class AccountApiControllerIntegrationTests : BaseApiIntegrationTests
    {
        public AccountApiControllerIntegrationTests(WebStoreApiFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task LoginReturnsTokenTest()
        {
            using var client = Factory.CreateAnonymousClient();

            var response = await WebStoreApiFactory.LoginAsync(client, TestUserSeeder.TestUserEmail, TestUserSeeder.TestUserPassword);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var loginResponse = await response.Content.ReadFromJsonAsync<WebStoreApiFactory.LoginResponse>();
            Assert.NotNull(loginResponse);
            Assert.False(string.IsNullOrWhiteSpace(loginResponse!.AccessToken));
            Assert.Matches(new Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$"), loginResponse.ExpiresIn);

            var token = new JwtSecurityTokenHandler().ReadJwtToken(loginResponse.AccessToken);
            Assert.Equal(token.ValidTo.ToString("yyyy-MM-ddTHH:mm:ss"), loginResponse.ExpiresIn);
        }

        [Fact]
        public async Task TokenContainsRequiredClaimsAndSignatureTest()
        {
            using var client = Factory.CreateAnonymousClient();
            var accessToken = await WebStoreApiFactory.GetTokenAsync(client, TestUserSeeder.TestUserEmail, TestUserSeeder.TestUserPassword);
            var jwtOptions = Factory.Services.GetRequiredService<IOptions<JwtOptionsVm>>().Value;

            var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
            Assert.Equal(TestUserSeeder.TestUserEmail, token.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
            Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Nbf);
            Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Exp);
            Assert.DoesNotContain(token.Claims, c => c.Type == ClaimTypes.Role);

            var validationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                ValidateLifetime = true
            };
            new JwtSecurityTokenHandler().ValidateToken(accessToken, validationParameters, out var validatedToken);
            Assert.NotNull(validatedToken);
        }

        [Fact]
        public async Task TokenContainsUserRolesTest()
        {
            using var client = Factory.CreateAnonymousClient();
            var accessToken = await WebStoreApiFactory.GetTokenAsync(client, WebStoreApiFactory.UserWithRoleLogin, WebStoreApiFactory.UserWithRolePassword);

            var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var roleClaim = Assert.Single(token.Claims, c => c.Type == ClaimTypes.Role);
            Assert.Equal(WebStoreApiFactory.UserWithRoleRole, roleClaim.Value);
        }

        [Fact]
        public async Task TokenExpiresAfterConfiguredMinutesInUtcTest()
        {
            using var client = Factory.CreateAnonymousClient();
            var jwtOptions = Factory.Services.GetRequiredService<IOptions<JwtOptionsVm>>().Value;
            Assert.Equal(20, jwtOptions.TokenExpirationMinutes);

            var issuedAtUtc = DateTimeOffset.UtcNow;
            var accessToken = await WebStoreApiFactory.GetTokenAsync(client, TestUserSeeder.TestUserEmail, TestUserSeeder.TestUserPassword);

            var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var expectedExp = issuedAtUtc.AddMinutes(jwtOptions.TokenExpirationMinutes).ToUnixTimeSeconds();
            var actualExp = long.Parse(token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Exp).Value);
            Assert.InRange(actualExp, expectedExp - 5, expectedExp + 5);
            var nbf = long.Parse(token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Nbf).Value);
            Assert.InRange(nbf, issuedAtUtc.ToUnixTimeSeconds() - 5, issuedAtUtc.ToUnixTimeSeconds() + 5);
        }

        [Fact]
        public async Task TokenAuthorizesProtectedEndpointTest()
        {
            using var client = await Factory.CreateAuthorizedClientAsync();

            var response = await client.GetAsync("/api/ProductApi");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task InvalidTokenIsRejectedTest()
        {
            using var client = Factory.CreateAnonymousClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "nieprawidlowy.token.jwt");

            var response = await client.GetAsync("/api/ProductApi");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("abc", "Test1234")]
        [InlineData("test.user@webstore.pl", "abc")]
        [InlineData("test.user@webstore.pl", "")]
        public async Task LoginWithInvalidModelReturnsBadRequestTest(string login, string password)
        {
            using var client = Factory.CreateAnonymousClient();

            var response = await WebStoreApiFactory.LoginAsync(client, login, password);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Invalid credentials", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task LoginWithWrongPasswordReturnsBadRequestTest()
        {
            using var client = Factory.CreateAnonymousClient();

            var response = await WebStoreApiFactory.LoginAsync(client, TestUserSeeder.TestUserEmail, "ZleHaslo999");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Invalid credentials", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task LoginWithUnknownUserReturnsBadRequestTest()
        {
            using var client = Factory.CreateAnonymousClient();

            var response = await WebStoreApiFactory.LoginAsync(client, "nieznany@eg.eg", "User1234");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Invalid credentials", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task LoginExceptionReturnsBadRequestAndLogsErrorTest()
        {
            using var client = Factory.CreateAnonymousClient();

            var response = await WebStoreApiFactory.LoginAsync(client, WebStoreApiFactory.UserWithoutEmailLogin, WebStoreApiFactory.UserWithoutEmailPassword);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Error occurred", await response.Content.ReadAsStringAsync());
            Assert.Contains(Factory.LoggerProvider.Entries, e => e.Level == LogLevel.Error && e.Exception != null);
        }

        [Fact]
        public async Task PasswordIsNotLoggedTest()
        {
            const string wrongPassword = "TajneHasloXyz987";
            using var client = Factory.CreateAnonymousClient();

            await WebStoreApiFactory.LoginAsync(client, TestUserSeeder.TestUserEmail, wrongPassword);
            await WebStoreApiFactory.LoginAsync(client, "abc", wrongPassword);
            await WebStoreApiFactory.LoginAsync(client, TestUserSeeder.TestUserEmail, TestUserSeeder.TestUserPassword);
            await WebStoreApiFactory.LoginAsync(client, WebStoreApiFactory.UserWithoutEmailLogin, WebStoreApiFactory.UserWithoutEmailPassword);

            var entries = Factory.LoggerProvider.Entries;
            Assert.Contains(entries, e => e.Message.Contains(TestUserSeeder.TestUserEmail));
            foreach (var password in new[] { wrongPassword, TestUserSeeder.TestUserPassword, WebStoreApiFactory.UserWithoutEmailPassword })
            {
                Assert.DoesNotContain(entries, e => e.Message.Contains(password));
                Assert.DoesNotContain(entries, e => e.Exception != null && e.Exception.ToString().Contains(password));
            }
        }
    }
}
