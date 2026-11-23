using System.ComponentModel.DataAnnotations;
using LangCenter.API.Security;
using LangCenter.Application.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/classes/{classId:guid}/enrollments")]
[Authorize(Policy = Policies.ManageClasses)]
public class EnrollmentsController(EnrollStudentCommandHandler enrollStudent) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<EnrollmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrollmentDto>> Create(Guid classId, EnrollStudentRequest request, CancellationToken ct)
    {
        var enrollment = await enrollStudent.HandleAsync(new EnrollStudentCommand(classId, request.StudentId!.Value), ct);
        return StatusCode(StatusCodes.Status201Created, enrollment);
    }
}

public record EnrollStudentRequest([Required] Guid? StudentId);
