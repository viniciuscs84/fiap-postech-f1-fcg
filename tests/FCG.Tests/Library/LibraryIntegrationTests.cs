using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FCG.Domain.Library;
using FCG.Infrastructure.Persistence;
using FCG.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Tests.Library;

public sealed class LibraryIntegrationTests
{
    [Fact]
    public async Task User_can_associate_catalog_game_with_own_library()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var user = await RegisterUserAsync(client, "Alice", "alice@example.com");
        var adminToken = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, adminToken);
        var game = await CreateGameAsync(adminClient, "Game One", "First game", "Action");

        var userToken = await LoginAsync(client, "alice@example.com", "Password1!");
        var userClient = CreateAuthenticatedClient(factory, userToken);

        var response = await userClient.PostAsync($"/api/library/me/games/{game.Id}", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LibraryItemResponse>();
        Assert.Equal(game.Id, payload?.GameId);
        Assert.Equal("Game One", payload?.Title);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var acquisition = await dbContext.AcquiredGames.SingleAsync();
        Assert.Equal(user.Id, acquisition.UserId);
        Assert.Equal(game.Id, acquisition.GameId);
    }

    [Fact]
    public async Task User_cannot_associate_same_game_twice()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        await RegisterUserAsync(client, "Alice", "alice@example.com");
        var adminToken = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, adminToken);
        var game = await CreateGameAsync(adminClient, "Game One", "First game", "Action");

        var userToken = await LoginAsync(client, "alice@example.com", "Password1!");
        var userClient = CreateAuthenticatedClient(factory, userToken);

        var firstResponse = await userClient.PostAsync($"/api/library/me/games/{game.Id}", null);
        var secondResponse = await userClient.PostAsync($"/api/library/me/games/{game.Id}", null);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Unknown_game_cannot_be_associated_with_library()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        await RegisterUserAsync(client, "Alice", "alice@example.com");
        var userToken = await LoginAsync(client, "alice@example.com", "Password1!");
        var userClient = CreateAuthenticatedClient(factory, userToken);

        var response = await userClient.PostAsync($"/api/library/me/games/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Missing_token_is_rejected_for_game_acquisition_association()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/library/me/games/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_can_retrieve_only_owned_games()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var alice = await RegisterUserAsync(client, "Alice", "alice@example.com");
        var bob = await RegisterUserAsync(client, "Bob", "bob@example.com");

        var adminToken = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, adminToken);

        var firstGame = await CreateGameAsync(adminClient, "Game One", "First game", "Action");
        var secondGame = await CreateGameAsync(adminClient, "Game Two", "Second game", "RPG");

        await SeedOwnershipAsync(factory, alice.Id, firstGame.Id);
        await SeedOwnershipAsync(factory, bob.Id, secondGame.Id);

        var aliceToken = await LoginAsync(client, "alice@example.com", "Password1!");
        var aliceClient = CreateAuthenticatedClient(factory, aliceToken);
        var aliceResponse = await aliceClient.GetAsync("/api/library/me");

        aliceResponse.EnsureSuccessStatusCode();
        var aliceLibrary = await aliceResponse.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();
        Assert.Single(aliceLibrary!);
        Assert.Equal(firstGame.Id, aliceLibrary![0].GameId);
        Assert.Equal("Game One", aliceLibrary[0].Title);

        var bobToken = await LoginAsync(client, "bob@example.com", "Password1!");
        var bobClient = CreateAuthenticatedClient(factory, bobToken);
        var bobResponse = await bobClient.GetAsync("/api/library/me");

        bobResponse.EnsureSuccessStatusCode();
        var bobLibrary = await bobResponse.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();
        Assert.Single(bobLibrary!);
        Assert.Equal(secondGame.Id, bobLibrary![0].GameId);
        Assert.Equal("Game Two", bobLibrary[0].Title);
    }

    [Fact]
    public async Task User_without_owned_games_receives_empty_library()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        await RegisterUserAsync(client, "Carol", "carol@example.com");
        var token = await LoginAsync(client, "carol@example.com", "Password1!");
        var authenticatedClient = CreateAuthenticatedClient(factory, token);

        var response = await authenticatedClient.GetAsync("/api/library/me");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();
        Assert.Empty(payload ?? []);
    }

    [Fact]
    public async Task Missing_token_is_rejected_for_library_endpoint()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/library/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_see_other_users_owned_games()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var alice = await RegisterUserAsync(client, "Alice", "alice@example.com");
        await RegisterUserAsync(client, "Bob", "bob@example.com");

        var adminToken = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, adminToken);
        var game = await CreateGameAsync(adminClient, "Shared Game", "Owned by Alice", "Adventure");

        await SeedOwnershipAsync(factory, alice.Id, game.Id);

        var bobToken = await LoginAsync(client, "bob@example.com", "Password1!");
        var bobClient = CreateAuthenticatedClient(factory, bobToken);

        var response = await bobClient.GetAsync("/api/library/me");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();
        Assert.Empty(payload ?? []);
    }

    private static HttpClient CreateAuthenticatedClient(TestAppFactory factory, string accessToken)
    {
        var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return authenticatedClient;
    }

    private static async Task<RegisteredUserResponse> RegisterUserAsync(HttpClient client, string name, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name,
            email,
            password = "Password1!"
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisteredUserResponse>())!;
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

    private static async Task<RegisteredGameResponse> CreateGameAsync(HttpClient client, string title, string description, string genre)
    {
        var response = await client.PostAsJsonAsync("/api/admin/games", new
        {
            title,
            description,
            genre
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisteredGameResponse>())!;
    }

    private static async Task SeedOwnershipAsync(TestAppFactory factory, Guid userId, Guid gameId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        dbContext.AcquiredGames.Add(AcquiredGame.Acquire(userId, gameId, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class RegisteredUserResponse
    {
        public Guid Id { get; init; }
    }

    private sealed class RegisteredGameResponse
    {
        public Guid Id { get; init; }
    }

    private sealed class LibraryItemResponse
    {
        public Guid GameId { get; init; }

        public string Title { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string Genre { get; init; } = string.Empty;

        public DateTime AcquiredAtUtc { get; init; }
    }
}
