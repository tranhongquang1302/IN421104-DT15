using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LangCenter.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(LoginCommandHandler login) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType<LoginResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await login.HandleAsync(new LoginCommand(request.Email, request.Password), ct);
        return result is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Đăng nhập thất bại",
                detail: "Email hoặc mật khẩu không đúng.")
            : Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentAccountResponse> GetCurrentAccount() => Ok(new CurrentAccountResponse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub)!,
        User.FindFirstValue(JwtRegisteredClaimNames.Email)!,
        User.FindAll("role").Select(claim => claim.Value).ToArray()));
}

public record LoginRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(256)] string Password);

public record CurrentAccountResponse(string Id, string Email, IReadOnlyList<string> Roles);
