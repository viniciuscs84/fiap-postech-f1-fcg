using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FCG.Domain.Users;

namespace FCG.Tests.Users;

public sealed class UserAdministrationIntegrationTests
{
    [Fact]
    public async Task Administrator_can_list_and_get_users()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        var registeredUser = await RegisterUserAsync(client, "Alice", "alice@example.com");
        var adminClient = await CreateAdministratorClientAsync(factory);

        var listResponse = await adminClient.GetAsync("/api/admin/users");
        listResponse.EnsureSuccessStatusCode();
        var users = await listResponse.Content.ReadFromJsonAsync<List<AdminUserResponse>>();

        Assert.Contains(users ?? [], user => user.Id == registeredUser.Id && user.Role == UserRole.User.ToString());

        var getResponse = await adminClient.GetAsync($"/api/admin/users/{registeredUser.Id}");
        getResponse.EnsureSuccessStatusCode();
        var userDetails = await getResponse.Content.ReadFromJsonAsync<AdminUserResponse>();

        Assert.Equal(registeredUser.Id, userDetails?.Id);
        Assert.Equal("alice@example.com", userDetails?.Email);
    }

    [Fact]
    public async Task Administrator_can_change_user_role()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        var registeredUser = await RegisterUserAsync(client, "Alice", "alice@example.com");
        var adminClient = await CreateAdministratorClientAsync(factory);

        var response = await adminClient.PatchAsJsonAsync(
            $"/api/admin/users/{registeredUser.Id}/role",
            new { role = "Administrator" });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AdminUserResponse>();
        Assert.Equal(UserRole.Administrator.ToString(), payload?.Role);

        var promotedUserClient = await CreateAuthenticatedClientAsync(factory, "alice@example.com", "Password1!");
        var adminEndpointResponse = await promotedUserClient.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.OK, adminEndpointResponse.StatusCode);
    }

    [Fact]
    public async Task Administrator_can_delete_user()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        var registeredUser = await RegisterUserAsync(client, "Alice", "alice@example.com");
        var adminClient = await CreateAdministratorClientAsync(factory);

        var deleteResponse = await adminClient.DeleteAsync($"/api/admin/users/{registeredUser.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await adminClient.GetAsync($"/api/admin/users/{registeredUser.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Regular_user_cannot_manage_users()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        var registeredUser = await RegisterUserAsync(client, "Alice", "alice@example.com");
        var userClient = await CreateAuthenticatedClientAsync(factory, "alice@example.com", "Password1!");

        Assert.Equal(HttpStatusCode.Forbidden, (await userClient.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await userClient.GetAsync($"/api/admin/users/{registeredUser.Id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await userClient.PatchAsJsonAsync($"/api/admin/users/{registeredUser.Id}/role", new { role = "Administrator" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await userClient.DeleteAsync($"/api/admin/users/{registeredUser.Id}")).StatusCode);
    }

    [Fact]
    public async Task Administrator_cannot_change_own_role_or_delete_own_account()
    {
        await using var factory = new TestAppFactory();
        var adminClient = await CreateAdministratorClientAsync(factory);
        var users = await adminClient.GetFromJsonAsync<List<AdminUserResponse>>("/api/admin/users");
        var administrator = Assert.Single(users!, user => user.Email == "admin@example.com");

        var roleResponse = await adminClient.PatchAsJsonAsync(
            $"/api/admin/users/{administrator.Id}/role",
            new { role = "User" });
        Assert.Equal(HttpStatusCode.Conflict, roleResponse.StatusCode);

        var deleteResponse = await adminClient.DeleteAsync($"/api/admin/users/{administrator.Id}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Invalid_role_is_rejected()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();
        var registeredUser = await RegisterUserAsync(client, "Alice", "alice@example.com");
        var adminClient = await CreateAdministratorClientAsync(factory);

        var response = await adminClient.PatchAsJsonAsync(
            $"/api/admin/users/{registeredUser.Id}/role",
            new { role = "SuperUser" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private static async Task<HttpClient> CreateAdministratorClientAsync(TestAppFactory factory)
    {
        return await CreateAuthenticatedClientAsync(factory, "admin@example.com", "Admin123!");
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(TestAppFactory factory, string email, string password)
    {
        var loginClient = factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        loginResponse.EnsureSuccessStatusCode();
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!;

        var authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return authenticatedClient;
    }

    private sealed class RegisteredUserResponse
    {
        public Guid Id { get; init; }
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class AdminUserResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; }
    }
}
