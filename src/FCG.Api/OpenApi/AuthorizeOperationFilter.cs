using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FCG.Api.OpenApi;

/// <summary>Adds the bearer security requirement to protected OpenAPI operations.</summary>
public sealed class AuthorizeOperationFilter : IOperationFilter
{
    /// <summary>Applies the security requirement when endpoint metadata requires authorization.</summary>
    /// <param name="operation">OpenAPI operation being configured.</param>
    /// <param name="context">Operation metadata context.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresAuthorization = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (!requiresAuthorization)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        });
    }
}
