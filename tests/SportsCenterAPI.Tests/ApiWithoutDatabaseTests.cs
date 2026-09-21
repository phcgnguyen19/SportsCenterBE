using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SportsCenterAPI.Tests;

public sealed class NoDatabaseFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                // An intentionally unavailable database: these checks must not need one.
                ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1,1;Database=Unavailable;User ID=test;Password=test;Connect Timeout=1;Encrypt=False;",
                ["Database:AutoMigrate"] = "false",
                ["OpenApi:Enabled"] = "true",
                ["Jwt:SecretKey"] = "Api_Contract_Test_Only_Key_With_At_Least_32_Bytes",
                ["Jwt:Issuer"] = "ApiTests", ["Jwt:Audience"] = "ApiTests"
            }));
    }
}

public class ApiWithoutDatabaseTests(NoDatabaseFactory factory) : IClassFixture<NoDatabaseFactory>
{
    private HttpClient Client() => factory.CreateClient(new()
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });

    [Fact]
    public async Task ScalarAndOpenApiStartWithoutDatabaseAndDoNotRequireBearerForLogin()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/scalar/v1")).StatusCode);
        var response = await client.GetAsync("/openapi/v1.json");
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, text);
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        Assert.Equal("bearer", root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        var paths = root.GetProperty("paths");
        var login = paths.GetProperty("/api/auth/login").GetProperty("post");
        Assert.True(!login.TryGetProperty("security", out var loginSecurity) || loginSecurity.GetArrayLength() == 0);
        var register = paths.GetProperty("/api/auth/register").GetProperty("post");
        Assert.True(!register.TryGetProperty("security", out var registerSecurity) || registerSecurity.GetArrayLength() == 0);
        Assert.True(paths.GetProperty("/api/member-subscriptions").GetProperty("post").GetProperty("security")[0].TryGetProperty("Bearer", out _));
    }

    [Theory]
    [InlineData("POST", "/api/membership-packages")]
    [InlineData("PUT", "/api/membership-packages/1")]
    [InlineData("DELETE", "/api/membership-packages/1")]
    [InlineData("GET", "/api/membership-packages/manage")]
    [InlineData("POST", "/api/member-subscriptions")]
    [InlineData("GET", "/api/member-subscriptions/mine")]
    [InlineData("POST", "/api/member-subscriptions/1/payments")]
    [InlineData("POST", "/api/member-subscriptions/1/cancel")]
    [InlineData("POST", "/api/members")]
    [InlineData("GET", "/api/users/staff")]
    public async Task ProtectedRoutesRejectMissingTokenBeforeAccessingDatabase(string method, string route)
    {
        using var client = Client();
        using var request = new HttpRequestMessage(new HttpMethod(method), route) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task InvalidTokenIsRejectedBeforeDatabaseAccess()
    {
        using var client = Client();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/member-subscriptions/mine")).StatusCode);
    }

    [Theory]
    [InlineData("/api/auth/register")]
    [InlineData("/api/auth/login")]
    public async Task InvalidPublicRequestsReturnValidationErrorsWithoutDatabase(string route)
    {
        using var client = Client();
        var response = await client.PostAsJsonAsync(route, new { email = "not-an-email", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("errors", out _));
    }
}
