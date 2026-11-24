using LangCenter.Application.Common;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LangCenter.API.Pages.Workspace;

public abstract class PortalPageModel : PageModel
{
    protected async Task<bool> TrySaveAsync(Func<Task> command)
    {
        if (!ModelState.IsValid)
            return false;

        try
        {
            await command();
            return true;
        }
        catch (AppException error)
        {
            ModelState.AddModelError(string.Empty, error.Message);
            return false;
        }
    }
}
