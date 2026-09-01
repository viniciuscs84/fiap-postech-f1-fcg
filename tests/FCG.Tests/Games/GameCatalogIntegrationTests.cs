using System.Net;
using System.Net.Http.Json;
using FCG.Infrastructure.Persistence;
using FCG.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Tests.Games;

public sealed class GameCatalogIntegrationTests
{
    [Fact]
    public async Task Administrator_can_register_game()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@example.com",
            password = "Admin123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var token = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/admin/games", new
        {
            title = "  Halo  ",
            description = "  A sci-fi shooter  ",
            genre = "  Shooter  "
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GameResponse>();
        Assert.Equal("Halo", payload?.Title);
        Assert.Equal("A sci-fi shooter", payload?.Description);
        Assert.Equal("Shooter", payload?.Genre);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storedGame = await dbContext.Games.SingleAsync();
        Assert.Equal("Halo", storedGame.Title);
        Assert.Equal("Shooter", storedGame.Genre);
    }

    [Fact]
    public async Task Regular_user_is_denied_game_registration()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Alice",
            email = "alice@example.com",
            password = "Password1!"
        });

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "alice@example.com",
            password = "Password1!"
        });

        var token = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/admin/games", new
        {
            title = "Game Title",
            description = "Game Description",
            genre = "Action"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Game Description", "Action")]
    [InlineData("Game Title", "", "Action")]
    [InlineData("Game Title", "Game Description", "")]
    public async Task Invalid_required_data_is_rejected(string title, string description, string genre)
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@example.com",
            password = "Admin123!"
        });

        var token = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/admin/games", new
        {
            title,
            description,
            genre
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class GameResponse
    {
        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Genre { get; init; } = string.Empty;
    }
}
