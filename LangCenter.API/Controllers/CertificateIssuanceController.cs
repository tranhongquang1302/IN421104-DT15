using System.ComponentModel.DataAnnotations;
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
public class CertificateIssuanceController(IssueCertificateCommandHandler handler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CertificateDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CertificateDto>> Issue(Guid id, IssueCertificateRequest request, CancellationToken ct)
    {
        var certificate = await handler.HandleAsync(new IssueCertificateCommand(id, request.CompletionConfirmed!.Value), ct);
        return StatusCode(StatusCodes.Status201Created, certificate);
    }
}

public record IssueCertificateRequest([Required] bool? CompletionConfirmed);
