using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Security;

public static class KeycloakAuthenticationExtensions
{
    public static WebApplicationBuilder AddKeycloakAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["Keycloak:Authority"];
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidIssuers = builder.Configuration.GetSection("Keycloak:ValidIssuers").Get<string[]>(),
                    RoleClaimType = KeycloakClaimTypes.Roles,
                    NameClaimType = KeycloakClaimTypes.PreferredUsername
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
