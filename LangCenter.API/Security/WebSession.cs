using System.Security.Claims;
using LangCenter.Application.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace LangCenter.API.Security;

public static class WebSession
{
    public const string Scheme = "LangCenterWeb";
    public const string Policy = "WebSession";

    public static void Configure(IServiceCollection services, bool development)
    {
        services.AddAuthentication().AddCookie(Scheme, options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/forbidden";
            options.Cookie.Name = "LangCenter.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(2);
        });
        services.AddAuthorization(options => options.AddPolicy(Policy, policy =>
            policy.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser()));
    }

    public static ClaimsPrincipal FromAccount(StaffAccount account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Email),
            new(ClaimTypes.Email, account.Email)
        };
        claims.AddRange(account.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
    }
}
