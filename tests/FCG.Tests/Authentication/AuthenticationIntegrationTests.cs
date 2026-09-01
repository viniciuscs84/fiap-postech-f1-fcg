using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FCG.Application.Authentication;
using FCG.Tests;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FCG.Tests.Authentication;

public sealed class AuthenticationIntegrationTests
{
    [Fact]
    public async Task User_can_register_login_and_access_empty_own_library()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "  Alice  ",
            email = "alice@example.com",
            password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "alice@example.com",
            password = "Password1!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(loginPayload?.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.AccessToken);
        var libraryResponse = await client.GetAsync("/api/library/me");

        libraryResponse.EnsureSuccessStatusCode();
        var libraryPayload = await libraryResponse.Content.ReadFromJsonAsync<List<LibraryItemResponse>>();
        Assert.Empty(libraryPayload ?? []);
    }

    [Fact]
    public async Task User_token_is_denied_for_administrator_only_endpoint()
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/api/admin/games", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_token_is_allowed_for_administrator_only_endpoint()
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/admin/games", new
        {
            title = "Game Title",
            description = "Game Description",
            genre = "Action"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_credentials_return_unauthorized()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@example.com",
            password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Missing_token_is_rejected_for_protected_endpoint()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/library/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
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
