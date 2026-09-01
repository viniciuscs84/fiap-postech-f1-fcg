using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FCG.Application.Authentication;
using FCG.Application.Library;
using FCG.Infrastructure.Persistence;
using FCG.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FCG.Tests.Api;

public sealed class ApiFoundationIntegrationTests
{
    [Fact]
    public async Task Swagger_document_exposes_the_public_contract_and_correlation_header()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/auth/register", out _));
        Assert.True(paths.TryGetProperty("/api/auth/login", out _));
        Assert.True(paths.TryGetProperty("/api/library/me", out _));
        Assert.True(paths.TryGetProperty("/api/admin/games", out _));
        Assert.True(paths.TryGetProperty("/api/admin/promotions", out _));
    }

    [Fact]
    public async Task Unhandled_exceptions_are_translated_into_problem_details()
    {
        await using var baseFactory = new TestAppFactory();
        var factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILibraryService>();
                services.AddScoped<ILibraryService, ThrowingLibraryService>();
            });
        });

        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@example.com",
            password = "Admin123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var token = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/library/me");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));

        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(500, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Internal Server Error", problem.RootElement.GetProperty("title").GetString());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    [Fact]
    public async Task Protected_requests_still_emit_a_correlation_header_when_rejected()
    {
        await using var factory = new TestAppFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/library/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
    }

    private sealed class ThrowingLibraryService : ILibraryService
    {
        public Task<IReadOnlyList<LibraryItemResponse>> GetMyLibraryAsync(Guid userId, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated infrastructure failure.");
        }
    }
}
