using System.ComponentModel.DataAnnotations;
using LangCenter.API.Certificates;
using LangCenter.API.Security;
using LangCenter.Application.Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/enrollments/{id:guid}/certificate")]
[Authorize(Policy = Policies.ManageCertificates)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class CertificateIssuanceController(IssueCertificateCommandHandler handler, CertificateLinks links) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CertificateResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CertificateResponse>> Issue(Guid id, IssueCertificateRequest request, CancellationToken ct)
    {
        var certificate = await handler.HandleAsync(new IssueCertificateCommand(id, request.CompletionConfirmed!.Value), ct);
        var verifyUrl = links.GetVerificationUrl(certificate.LookupCode);
        var response = new CertificateResponse(certificate.Id, certificate.EnrollmentId, certificate.LookupCode,
            certificate.IssuedAt, verifyUrl);
        return Created(verifyUrl.AbsoluteUri, response);
    }
}

public record IssueCertificateRequest([Required] bool? CompletionConfirmed);

public record CertificateResponse(Guid Id, Guid EnrollmentId, string LookupCode, DateTime IssuedAt, Uri VerifyUrl);
