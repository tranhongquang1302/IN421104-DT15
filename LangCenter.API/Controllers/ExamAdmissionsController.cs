using LangCenter.API.Security;
using LangCenter.Application.Exams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/enrollments/{id:guid}/exam-admission")]
[Authorize(Policy = Policies.ConfirmExamAdmission)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ExamAdmissionsController(ConfirmExamAdmissionCommandHandler handler) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ExamAdmissionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExamAdmissionDto>> Confirm(Guid id, CancellationToken ct)
    {
        var admission = await handler.HandleAsync(new ConfirmExamAdmissionCommand(id), ct);
        return StatusCode(StatusCodes.Status201Created, admission);
    }
}
