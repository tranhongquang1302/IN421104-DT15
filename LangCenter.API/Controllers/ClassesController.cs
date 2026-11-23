using System.ComponentModel.DataAnnotations;
using LangCenter.API.Security;
using LangCenter.Application.Classes;
using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/classes")]
[Authorize(Policy = Policies.ManageClasses)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ClassesController(
    CreateClassCommandHandler createClass,
    GetClassesQueryHandler getClasses,
    GetClassByIdQueryHandler getClassById) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TrainingClassDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TrainingClassDto>> Create(CreateClassRequest request, CancellationToken ct)
    {
        var trainingClass = await createClass.HandleAsync(
            new CreateClassCommand(request.Name, request.MinimumEnrollment, request.TuitionFee!.Value), ct);
        return CreatedAtAction(nameof(GetById), new { id = trainingClass.Id }, trainingClass);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TrainingClassDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrainingClassDto>>> GetAll(CancellationToken ct) =>
        Ok(await getClasses.HandleAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TrainingClassDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingClassDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await getClassById.HandleAsync(new GetClassByIdQuery(id), ct));
}

public record CreateClassRequest(
    [Required, StringLength(TrainingClass.MaxNameLength)] string Name,
    [Range(1, int.MaxValue)] int MinimumEnrollment,
    [Required] decimal? TuitionFee);
