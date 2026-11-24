using LangCenter.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LangCenter.API.Pages;

[Authorize(Policy = WebSession.Policy)]
public class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Workspace/Index");
}
