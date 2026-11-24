using LangCenter.API.Certificates;
using LangCenter.API.Security;
using LangCenter.Application.Certificates;
using LangCenter.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/certificates")]
public class CertificatesController(ICertificateVerificationQuery verification, GetCertificateQueryHandler getCertificate,
    CertificateLinks links, IQrCodeRenderer qrCodes) : ControllerBase
{
    [HttpGet("{id:guid}/qr")]
    [Authorize(Policy = Policies.ManageCertificates)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [Produces("image/png")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQr(Guid id, CancellationToken ct)
    {
        var certificate = await getCertificate.HandleAsync(new GetCertificateQuery(id), ct);
        var url = links.GetVerificationUrl(certificate.LookupCode);
        return File(qrCodes.RenderPng(url.AbsoluteUri), "image/png");
    }

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
