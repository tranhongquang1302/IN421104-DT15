using System.ComponentModel.DataAnnotations;
using LangCenter.API.Security;
using LangCenter.Application.Common;
using LangCenter.Application.Students;
using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Policy = Policies.ManageStudents)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class StudentsController(
    CreateStudentCommandHandler createStudent,
    GetStudentsQueryHandler getStudents,
    GetStudentByIdQueryHandler getStudentById) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<StudentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentDto>> Create(CreateStudentRequest request, CancellationToken ct)
    {
        var student = await createStudent.HandleAsync(new CreateStudentCommand(request.FullName, request.Email), ct);
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StudentDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        if (page is null && pageSize is null)
            return Ok(await getStudents.HandleAsync(ct));

        var paging = PageRequest.Of(page ?? 1, pageSize ?? PageRequest.DefaultPageSize);
        return Ok(await getStudents.HandleAsync(paging, ct));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<StudentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await getStudentById.HandleAsync(new GetStudentByIdQuery(id), ct));
}

public record CreateStudentRequest(
    [Required, StringLength(Student.MaxFullNameLength)] string FullName,
    [Required, EmailAddress, StringLength(Student.MaxEmailLength)] string Email);
