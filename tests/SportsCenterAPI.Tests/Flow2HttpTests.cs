using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SportsCenterAPI.Helpers;
using SportsCenterAPI.Models;

namespace SportsCenterAPI.Tests;

public class Flow2HttpTests(Flow2Database database) : IClassFixture<Flow2Database>
{
    private sealed class Factory(string connection, TestClock clock) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection,
                ["Jwt:SecretKey"] = Secret, ["Jwt:Issuer"] = "Flow2Tests", ["Jwt:Audience"] = "Flow2Tests"
            }));
            builder.ConfigureServices(services => { services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(clock); });
        }
    }
    private const string Secret = "Flow2_HTTP_Integration_Only_Secret_At_Least_32_Bytes";
    private static HttpClient Client(Factory factory, User user)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtHelper.GenerateToken(user, Secret, "Flow2Tests", "Flow2Tests", 60));
        return client;
    }
    private static async Task<JsonElement> Data(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{response.StatusCode}: {content}");
        using var document = JsonDocument.Parse(content);
        return document.RootElement.GetProperty("data").Clone();
    }
    [LocalDbFact]
    public async Task RealHttpPipelineSupportsManagementBookingCheckInAndReview()
    {
        var clock = new TestClock();
        User Account(string role) => new() { Email = Guid.NewGuid().ToString("N") + "@example.test", Role = role, FullName = role, PasswordHash = "not-used", IsActive = true };
        var manager = Account("Manager"); var member = Account("Member"); member.Member = new Member();
        var coach = Account("Coach"); coach.Coach = new Coach();
        await using (var db = database.Open()) { db.AddRange(manager, member, coach); await db.SaveChangesAsync(); }
        using var factory = new Factory(database.ConnectionString, clock);
        using var admin = Client(factory, manager); using var customer = Client(factory, member); using var trainer = Client(factory, coach);
        var sport = await Data(await admin.PostAsJsonAsync("/api/sports", new { name = "HTTP test sport" }));
        var course = await Data(await admin.PostAsJsonAsync("/api/classes", new
        {
            className = "HTTP test class", sportId = sport.GetProperty("id").GetInt32(), coachId = coach.Coach.Id,
            maxCapacity = 5, price = 0, startDate = clock.UtcNow, endDate = clock.UtcNow.AddDays(30)
        }), HttpStatusCode.Created);
        var classId = course.GetProperty("id").GetInt32();
        var session = await Data(await admin.PostAsJsonAsync($"/api/classes/{classId}/sessions", new
        {
            coachId = coach.Coach.Id, cancellationPolicyId = 1, capacity = 5,
            startsAt = clock.UtcNow.AddDays(1), endsAt = clock.UtcNow.AddDays(1).AddHours(1)
        }), HttpStatusCode.Created);
        var sessionId = session.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.PostAsJsonAsync("/api/classes", new { })).StatusCode);
        var search = await Data(await customer.GetAsync($"/api/classes?search=HTTP&sportId={sport.GetProperty("id")}&onlyAvailable=true"));
        Assert.Equal(1, search.GetProperty("total").GetInt32());
        var coachSessions = await Data(await trainer.GetAsync("/api/class-sessions/mine"));
        Assert.Equal(1, coachSessions.GetProperty("total").GetInt32());
        var registration = await Data(await customer.PostAsync($"/api/class-sessions/{sessionId}/registrations", null), HttpStatusCode.Created);
        var registrationId = registration.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsync($"/api/class-sessions/{sessionId}/registrations", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync($"/api/class-sessions/{sessionId}/roster")).StatusCode);
        var roster = await Data(await trainer.GetAsync($"/api/class-sessions/{sessionId}/roster"));
        Assert.Equal(1, roster.GetArrayLength());
        var notifications = await Data(await customer.GetAsync("/api/notifications?unreadOnly=true"));
        var notificationId = notifications.GetProperty("items")[0].GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NotFound, (await trainer.PatchAsync($"/api/notifications/{notificationId}/read", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await customer.PatchAsync($"/api/notifications/{notificationId}/read", null)).StatusCode);
        clock.UtcNow = clock.UtcNow.AddDays(1).AddMinutes(-10);
        await Data(await customer.PostAsync($"/api/class-registrations/{registrationId}/check-in", null));
        clock.UtcNow = clock.UtcNow.AddHours(2);
        Assert.Equal(HttpStatusCode.NoContent, (await trainer.PostAsync($"/api/class-sessions/{sessionId}/complete", null)).StatusCode);
        var review = await Data(await customer.PostAsJsonAsync($"/api/class-registrations/{registrationId}/review", new { rating = 5, comment = "API works" }));
        Assert.Equal(5, review.GetProperty("rating").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync("/api/payments/vnpay/ipn")).StatusCode);
    }
}
