using FCG.Api.OpenApi;
using FCG.Api.Contracts;
using FCG.Api.Middleware;
using System.Text;
using FCG.Application.Authentication;
using FCG.Application.Library;
using FCG.Application.Games;
using FCG.Application.Promotions;
using FCG.Application.Users;
using FCG.Domain.Games;
using FCG.Domain.Promotions;
using FCG.Domain.Users;
using FCG.Infrastructure.Authentication;
using FCG.Infrastructure.Library;
using FCG.Infrastructure.Games;
using FCG.Infrastructure.Promotions;
using FCG.Infrastructure.Persistence;
using FCG.Infrastructure.Security;
using FCG.Infrastructure.Users;
using FCG.Migrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<BootstrapAdminOptions>(builder.Configuration.GetSection(BootstrapAdminOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=fcg.db";
    options.UseSqlite(connectionString, sqlite =>
        sqlite.MigrationsAssembly(typeof(MigrationAssemblyMarker).Assembly.GetName().Name));
});

builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IGameRepository, EfGameRepository>();
builder.Services.AddScoped<ILibraryRepository, EfLibraryRepository>();
builder.Services.AddScoped<IPromotionRepository, EfPromotionRepository>();
builder.Services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IGameRegistrationService, GameRegistrationService>();
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<IPromotionRegistrationService, PromotionRegistrationService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException("JWT signing key is not configured.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserOrAdministrator", policy =>
        policy.RequireRole(UserRole.User.ToString(), UserRole.Administrator.ToString()));
    options.AddPolicy("AdministratorOnly", policy =>
        policy.RequireRole(UserRole.Administrator.ToString()));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "FCG.Api.xml"));
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API FCG",
        Version = "v1",
        Description = "API REST da Fase 1 para contas de usuários, administração do catálogo de jogos, promoções e bibliotecas de jogos adquiridos."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe um token JWT no formato: Bearer {token}."
    });
    options.OperationFilter<AuthorizeOperationFilter>();
});

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API FCG v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "FCG API - Swagger";
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var bootstrapAdmin = scope.ServiceProvider.GetRequiredService<IOptions<BootstrapAdminOptions>>().Value;
    if (bootstrapAdmin.Enabled && !string.IsNullOrWhiteSpace(bootstrapAdmin.Email) && !string.IsNullOrWhiteSpace(bootstrapAdmin.Password))
    {
        var normalizedEmail = RegistrationRules.NormalizeEmail(bootstrapAdmin.Email);
        var existingAdmin = await dbContext.Users.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail && user.Role == UserRole.Administrator);
        if (existingAdmin is null)
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var admin = UserAccount.Register(
                RegistrationRules.NormalizeName(bootstrapAdmin.Name),
                bootstrapAdmin.Email,
                passwordHasher.HashPassword(bootstrapAdmin.Password),
                UserRole.Administrator,
                DateTime.UtcNow);

            dbContext.Users.Add(admin);
            await dbContext.SaveChangesAsync();
        }
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/register", async (
    RegisterUserCommand command,
    IUserRegistrationService registrationService,
    CancellationToken cancellationToken) =>
{
    var result = await registrationService.RegisterAsync(command, cancellationToken);

    return result switch
    {
        RegistrationOutcome.Success success => Results.Created($"/api/users/{success.User.Id}", success.User),
        RegistrationOutcome.ValidationFailure failure => Results.ValidationProblem(
            failure.Errors.ToDictionary(pair => pair.Key, pair => pair.Value)),
        RegistrationOutcome.Conflict => Results.Problem(
            detail: "Já existe uma conta com este e-mail.",
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflict"),
        _ => throw new InvalidOperationException("Unexpected registration outcome.")
    };
})
.WithTags("Autenticação")
.WithSummary("Cadastrar um novo usuário.")
.WithDescription("Cria um usuário com e-mail validado e senha que atende às regras de segurança da Fase 1.")
.Produces<RegisteredUserResponse>(StatusCodes.Status201Created)
.ProducesValidationProblem()
.ProducesProblem(StatusCodes.Status409Conflict)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPost("/api/auth/login", async (
    LoginCommand command,
    IAuthenticationService authenticationService,
    CancellationToken cancellationToken) =>
{
    var result = await authenticationService.LoginAsync(command, cancellationToken);

    return result switch
    {
        LoginOutcome.Success success => Results.Ok(new LoginResponseDto(success.AccessToken)),
        LoginOutcome.Failure => Results.Unauthorized(),
        _ => throw new InvalidOperationException("Unexpected login outcome.")
    };
})
.WithTags("Autenticação")
.WithSummary("Autenticar um usuário e emitir um JWT.")
.WithDescription("Valida as credenciais e retorna um token bearer contendo a identidade e o papel do usuário.")
.Produces<LoginResponseDto>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status500InternalServerError);

app.MapGet("/api/library/me", [Authorize(Policy = "UserOrAdministrator")] async (
    ClaimsPrincipal user,
    ILibraryService libraryService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdValue, out var userId))
    {
        logger.LogWarning("Library access rejected because the authenticated identity did not contain a valid user id claim.");
        return Results.Unauthorized();
    }

    var library = await libraryService.GetMyLibraryAsync(userId, cancellationToken);

    return Results.Ok(library);
})
.WithTags("Biblioteca")
.WithSummary("Consultar a biblioteca do usuário autenticado.")
.WithDescription("Retorna somente os jogos adquiridos pertencentes à identidade presente no token bearer.")
.Produces<IReadOnlyList<LibraryItemResponse>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/api/admin/games", [Authorize(Policy = "AdministratorOnly")] async (
    ClaimsPrincipal user,
    CreateGameCommand command,
    IGameRegistrationService gameRegistrationService,
    CancellationToken cancellationToken) =>
{
    var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdValue, out var creatorUserId))
    {
        return Results.Unauthorized();
    }

    var result = await gameRegistrationService.RegisterAsync(command, creatorUserId, cancellationToken);

    return result switch
    {
        GameRegistrationOutcome.Success success => Results.Created($"/api/admin/games/{success.Game.Id}", success.Game),
        GameRegistrationOutcome.ValidationFailure failure => Results.ValidationProblem(
            failure.Errors.ToDictionary(pair => pair.Key, pair => pair.Value)),
        _ => throw new InvalidOperationException("Unexpected game registration outcome.")
    };
})
.WithTags("Jogos")
.WithSummary("Cadastrar um novo jogo.")
.WithDescription("Cria um item no catálogo. Esta operação é restrita a administradores.")
.Produces<Game>(StatusCodes.Status201Created)
.ProducesValidationProblem()
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/api/admin/promotions", [Authorize(Policy = "AdministratorOnly")] async (
    ClaimsPrincipal user,
    CreatePromotionCommand command,
    IPromotionRegistrationService promotionRegistrationService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(userIdValue, out var creatorUserId))
    {
        logger.LogWarning("Promotion creation rejected because the authenticated identity did not contain a valid user id claim.");
        return Results.Unauthorized();
    }

    var result = await promotionRegistrationService.RegisterAsync(command, creatorUserId, cancellationToken);

    return result switch
    {
        PromotionRegistrationOutcome.Success success => Results.Created($"/api/admin/promotions/{success.Promotion.Id}", success.Promotion),
        PromotionRegistrationOutcome.ValidationFailure failure => Results.ValidationProblem(
            failure.Errors.ToDictionary(pair => pair.Key, pair => pair.Value)),
        PromotionRegistrationOutcome.Conflict => Results.Problem(
            detail: "Já existe uma promoção com este código.",
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflict"),
        _ => throw new InvalidOperationException("Unexpected promotion registration outcome.")
    };
})
.WithTags("Promoções")
.WithSummary("Cadastrar uma nova promoção.")
.WithDescription("Cria um registro administrativo de promoção. Esta operação é restrita a administradores.")
.Produces<Promotion>(StatusCodes.Status201Created)
.ProducesValidationProblem()
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.ProducesProblem(StatusCodes.Status409Conflict)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.Run();

/// <summary>Exposes the application entry point to integration tests.</summary>
public partial class Program
{
}
