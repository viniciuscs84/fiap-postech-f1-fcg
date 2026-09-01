using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FCG.Infrastructure.Persistence;
using FCG.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Tests.Promotions;

public sealed class PromotionAdministrationIntegrationTests
{
    [Fact]
    public async Task Administrator_can_create_promotion()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var token = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, token);

        var response = await adminClient.PostAsJsonAsync("/api/admin/promotions", new
        {
            name = "Launch Weekend",
            code = "LAUNCH25",
            discountPercentage = 25,
            startsAtUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            endsAtUtc = new DateTime(2026, 9, 15, 23, 59, 59, DateTimeKind.Utc)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PromotionResponse>();
        Assert.Equal("Launch Weekend", payload?.Name);
        Assert.Equal("LAUNCH25", payload?.Code);
        Assert.Equal(25m, payload?.DiscountPercentage);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedPromotion = await dbContext.Promotions.SingleAsync();
        Assert.Equal("LAUNCH25", storedPromotion.Code);
        Assert.Equal(25m, storedPromotion.DiscountPercentage);
    }

    [Fact]
    public async Task Regular_user_is_denied_promotion_creation()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        await RegisterUserAsync(client, "Alice", "alice@example.com");
        var token = await LoginAsync(client, "alice@example.com", "Password1!");
        var authenticatedClient = CreateAuthenticatedClient(factory, token);

        var response = await authenticatedClient.PostAsJsonAsync("/api/admin/promotions", new
        {
            name = "Launch Weekend",
            code = "LAUNCH25",
            discountPercentage = 25,
            startsAtUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            endsAtUtc = new DateTime(2026, 9, 15, 23, 59, 59, DateTimeKind.Utc)
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("", "PROMO10", 10)]
    [InlineData("Launch Weekend", "", 10)]
    [InlineData("Launch Weekend", "PROMO10", 0)]
    [InlineData("Launch Weekend", "PROMO10", 101)]
    public async Task Invalid_promotion_data_is_rejected(string name, string code, decimal discountPercentage)
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var token = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, token);

        var response = await adminClient.PostAsJsonAsync("/api/admin/promotions", new
        {
            name,
            code,
            discountPercentage,
            startsAtUtc = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            endsAtUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await dbContext.Promotions.ToListAsync());
    }

    private static HttpClient CreateAuthenticatedClient(TestAppFactory factory, string accessToken)
    {
        var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return authenticatedClient;
    }

    private static async Task RegisterUserAsync(HttpClient client, string name, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name,
            email,
            password = "Password1!"
        });

        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class PromotionResponse
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Code { get; init; } = string.Empty;

        public decimal DiscountPercentage { get; init; }
    }
}
