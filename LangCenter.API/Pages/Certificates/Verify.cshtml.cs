using LangCenter.Application.Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LangCenter.API.Pages.Certificates;

[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class VerifyModel(ICertificateVerificationQuery verification) : PageModel
{
    public CertificateVerificationDto? Certificate { get; private set; }
    public string? Code { get; private set; }

    public async Task OnGetAsync(string? lookupCode, [FromQuery] string? code, CancellationToken ct)
    {
        Code = (code ?? lookupCode)?.Trim();
        if (string.IsNullOrEmpty(Code))
            return;

        Certificate = await verification.FindByLookupCodeAsync(Code, ct);
        if (Certificate is null)
            Response.StatusCode = StatusCodes.Status404NotFound;
    }
}
