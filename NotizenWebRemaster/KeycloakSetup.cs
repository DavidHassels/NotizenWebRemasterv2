using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
namespace NotizenWebRemaster

{
    public static class KeycloakSetup
    {
        public static IServiceCollection AddKeycloakAuth(
            this IServiceCollection services, IConfiguration config )
        {
            var kc = config.GetSection("Keycloak");

            services.AddAuthentication(x =>
            {
                x.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(x =>
            {
                x.Cookie.HttpOnly = true;
                x.Cookie.SameSite = SameSiteMode.Lax;
                x.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401; return Task.CompletedTask;
                };
                x.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = 403; return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect("oidc", x =>
            {
                x.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                x.MetadataAddress = kc["Metadata"];
                x.ClientId = kc["ClientId"];
                x.ClientSecret = kc["ClientSecret"];
                x.ResponseType = "code";
                x.UsePkce = true;
                x.SaveTokens = true ;
                x.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
                x.RequireHttpsMetadata = false;
                x.CallbackPath = "/api/signin-oidc";
                x.SignedOutCallbackPath = "/api/signout-callback-oidc";
                x.MapInboundClaims = false;
                x.TokenValidationParameters.NameClaimType = "preferred_username";
            });

            services.AddAuthorization();

            return services;

        }
    }

    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapGet("/api/login", () =>
                Results.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    new[] { "oidc" }))
                .AllowAnonymous();

            app.MapGet("/api/logout", () =>
                Results.SignOut(
                    new AuthenticationProperties { RedirectUri = "/" },
                    new[] { CookieAuthenticationDefaults.AuthenticationScheme, "oidc" }));

            app.MapGet("/api/me", (ClaimsPrincipal user) =>
                Results.Ok(new
                {
                    name = user.Identity?.Name,
                    id = user.FindFirst("sub")?.Value
                }))
                .RequireAuthorization();
        }
    }
}
