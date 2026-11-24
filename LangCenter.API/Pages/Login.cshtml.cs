using System.ComponentModel.DataAnnotations;
using LangCenter.API.Security;
using LangCenter.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace LangCenter.API.Pages;

[AllowAnonymous]
[EnableRateLimiting("login")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class LoginModel(LoginCommandHandler login) : PageModel
{
    [BindProperty]
    public Credentials Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var session = await HttpContext.AuthenticateAsync(WebSession.Scheme);
        return session.Succeeded ? ContinueToWorkspace() : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        var account = await login.AuthenticateAsync(new LoginCommand(Input.Email, Input.Password), ct);
        if (account is null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return Page();
        }

        await HttpContext.SignInAsync(WebSession.Scheme, WebSession.FromAccount(account));
        return ContinueToWorkspace();
    }

    private IActionResult ContinueToWorkspace() => Url.IsLocalUrl(ReturnUrl)
        ? LocalRedirect(ReturnUrl!)
        : RedirectToPage("/Workspace/Index");

    public class Credentials
    {
        [Required(ErrorMessage = "Hãy nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hãy nhập mật khẩu."), StringLength(256)]
        public string Password { get; set; } = string.Empty;
    }
}
