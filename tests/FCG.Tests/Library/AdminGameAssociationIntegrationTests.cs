using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FCG.Tests;

namespace FCG.Tests.Library;

public sealed class AdminGameAssociationIntegrationTests
{
    [Fact]
    public async Task Administrator_can_associate_game_with_specific_user()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var user = await RegisterUserAsync(client, "Alice", "alice@example.com");
        var adminToken = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, adminToken);
        var game = await CreateGameAsync(adminClient, "Game One", "First game", "Action");

        var response = await adminClient.PostAsync($"/api/admin/users/{user.Id}/games/{game.Id}", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var userToken = await LoginAsync(client, "alice@example.com", "Password1!");
        var userClient = CreateAuthenticatedClient(factory, userToken);
        var libraryResponse = await userClient.GetAsync("/api/library/me");

        libraryResponse.EnsureSuccessStatusCode();
        var library = await libraryResponse.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();
        Assert.Single(library!);
        Assert.Equal(game.Id, library![0].GameId);
        Assert.Equal("Game One", library[0].Title);
    }

    [Fact]
    public async Task Regular_user_cannot_associate_game_with_another_user()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        await RegisterUserAsync(client, "Alice", "alice@example.com");
        var bob = await RegisterUserAsync(client, "Bob", "bob@example.com");

        var adminToken = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, adminToken);
        var game = await CreateGameAsync(adminClient, "Game One", "First game", "Action");

        var aliceToken = await LoginAsync(client, "alice@example.com", "Password1!");
        var aliceClient = CreateAuthenticatedClient(factory, aliceToken);

        var response = await aliceClient.PostAsync($"/api/admin/users/{bob.Id}/games/{game.Id}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_receives_not_found_for_unknown_target_user()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var adminToken = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, adminToken);
        var game = await CreateGameAsync(adminClient, "Game One", "First game", "Action");

        var response = await adminClient.PostAsync($"/api/admin/users/{Guid.NewGuid()}/games/{game.Id}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_cannot_associate_same_game_twice_with_target_user()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var user = await RegisterUserAsync(client, "Alice", "alice@example.com");
        var adminToken = await LoginAsync(client, "admin@example.com", "Admin123!");
        var adminClient = CreateAuthenticatedClient(factory, adminToken);
        var game = await CreateGameAsync(adminClient, "Game One", "First game", "Action");

        var firstResponse = await adminClient.PostAsync($"/api/admin/users/{user.Id}/games/{game.Id}", null);
        var secondResponse = await adminClient.PostAsync($"/api/admin/users/{user.Id}/games/{game.Id}", null);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
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
    }
}
