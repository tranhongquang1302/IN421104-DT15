using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LangCenter.Application.Auth;
using Microsoft.IdentityModel.Tokens;

namespace LangCenter.Infrastructure.Auth;

public record JwtSettings(string Issuer, string Audience, int ExpiryMinutes, SecurityKey SigningKey);

public class JwtTokenService(JwtSettings settings, TimeProvider clock) : ITokenService
{
    public LoginResult IssueAccessToken(StaffAccount account)
    {
        var now = clock.GetUtcNow();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };
        claims.AddRange(account.Roles.Select(role => new Claim("role", role)));

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(settings.ExpiryMinutes).UtcDateTime,
            signingCredentials: new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256));
        return new LoginResult(new JwtSecurityTokenHandler().WriteToken(token), settings.ExpiryMinutes * 60);
    }
}
