using LangCenter.Application.Certificates;
using LangCenter.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/certificates")]
public class CertificatesController(ICertificateVerificationQuery verification) : ControllerBase
{
    [HttpGet("verify/{lookupCode}")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<CertificateVerificationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CertificateVerificationDto>> Verify(string lookupCode, CancellationToken ct)
    {
        var certificate = await verification.FindByLookupCodeAsync(lookupCode, ct)
            ?? throw new NotFoundException("Không tìm thấy chứng chỉ.");
        return Ok(certificate);
    }
}
