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
    public async Task Authenticated_user_can_list_catalog_games()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        await RegisterUserAsync(client, "Alice", "alice@example.com");
        var adminToken = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, adminToken);

        await CreateGameAsync(adminClient, "Zelda", "Adventure game", "Adventure");
        await CreateGameAsync(adminClient, "Halo", "Sci-fi shooter", "Shooter");

        var userToken = await LoginAsync(client, "alice@example.com", "Password1!");
        var userClient = CreateAuthenticatedClient(factory, userToken);

        var response = await userClient.GetAsync("/api/games");

        response.EnsureSuccessStatusCode();
        var games = await response.Content.ReadFromJsonAsync<List<GameCatalogResponse>>();
        Assert.Equal(2, games?.Count);
        Assert.Equal("Halo", games![0].Title);
        Assert.Equal("Zelda", games[1].Title);
    }

    [Fact]
    public async Task Authenticated_user_can_get_catalog_game_by_id()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        await RegisterUserAsync(client, "Alice", "alice@example.com");
        var adminToken = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, adminToken);
        var game = await CreateGameAsync(adminClient, "Halo", "Sci-fi shooter", "Shooter");

        var userToken = await LoginAsync(client, "alice@example.com", "Password1!");
        var userClient = CreateAuthenticatedClient(factory, userToken);

        var response = await userClient.GetAsync($"/api/games/{game.Id}");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<GameCatalogResponse>();
        Assert.Equal(game.Id, payload?.Id);
        Assert.Equal("Halo", payload?.Title);
        Assert.Equal("Shooter", payload?.Genre);
    }

    [Fact]
    public async Task Catalog_game_query_returns_not_found_for_unknown_id()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        await RegisterUserAsync(client, "Alice", "alice@example.com");
        var token = await LoginAsync(client, "alice@example.com", "Password1!");
        var authenticatedClient = CreateAuthenticatedClient(factory, token);

        var response = await authenticatedClient.GetAsync($"/api/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Missing_token_is_rejected_for_catalog_query()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/games");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    private static HttpClient CreateAuthenticatedClient(TestAppFactory factory, string accessToken)
    {
        var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
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

    private static async Task<GameResponse> CreateGameAsync(HttpClient client, string title, string description, string genre)
    {
        var response = await client.PostAsJsonAsync("/api/admin/games", new
        {
            title,
            description,
            genre
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GameResponse>())!;
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class GameResponse
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Genre { get; init; } = string.Empty;
    }

    private sealed class GameCatalogResponse
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Genre { get; init; } = string.Empty;

        public DateTime CreatedAtUtc { get; init; }
    }
}
