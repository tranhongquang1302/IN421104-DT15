using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using LangCenter.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LangCenter.API.Security;

public static class JwtAuthentication
{
    public static void Configure(IServiceCollection services)
    {
        services.AddSingleton(provider => LoadSettings(
            provider.GetRequiredService<IConfiguration>(),
            provider.GetRequiredService<IHostEnvironment>()));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtSettings>((options, settings) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = settings.SigningKey,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = JwtRegisteredClaimNames.Email,
                    RoleClaimType = "role"
                };
            });
    }

    private static JwtSettings LoadSettings(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection("JwtSettings");
        var issuer = section["Issuer"] ?? "LangCenter";
        var audience = section["Audience"] ?? "LangCenter.Api";
        var expiryMinutes = section.GetValue("ExpiryMinutes", 30);

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || expiryMinutes is < 1 or > 60)
            throw new InvalidOperationException("JwtSettings requires an issuer, an audience, and a lifetime of 1–60 minutes.");

        var encodedKey = section["SigningKey"];
        var key = string.IsNullOrWhiteSpace(encodedKey) && environment.IsDevelopment()
            ? RandomNumberGenerator.GetBytes(32)
            : LoadSigningKey(encodedKey);
        return new JwtSettings(issuer, audience, expiryMinutes, new SymmetricSecurityKey(key));
    }

    private static byte[] LoadSigningKey(string? encodedKey)
    {
        var key = new byte[encodedKey?.Length ?? 0];
        if (string.IsNullOrWhiteSpace(encodedKey) ||
            !Convert.TryFromBase64String(encodedKey, key, out var length) || length < 32)
            throw new InvalidOperationException("Configure JwtSettings:SigningKey with at least 32 random bytes encoded as Base64.");

        return key[..length];
    }
}
