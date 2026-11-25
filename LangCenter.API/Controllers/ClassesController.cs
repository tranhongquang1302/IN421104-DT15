using System.ComponentModel.DataAnnotations;
using LangCenter.API.Security;
using LangCenter.Application.Classes;
using LangCenter.Application.Common;
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
    GetClassByIdQueryHandler getClassById,
    LaunchClassCommandHandler launchClass) : ControllerBase
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
    public async Task<IActionResult> GetAll(CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        if (page is null && pageSize is null)
            return Ok(await getClasses.HandleAsync(ct));

        var paging = PageRequest.Of(page ?? 1, pageSize ?? PageRequest.DefaultPageSize);
        return Ok(await getClasses.HandleAsync(paging, ct));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TrainingClassDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingClassDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await getClassById.HandleAsync(new GetClassByIdQuery(id), ct));

    [HttpPost("{id:guid}/launch")]
    [ProducesResponseType<TrainingClassDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingClassDto>> Launch(Guid id, CancellationToken ct) =>
        Ok(await launchClass.HandleAsync(new LaunchClassCommand(id), ct));
}

public record CreateClassRequest(
    [Required, StringLength(TrainingClass.MaxNameLength)] string Name,
    [Range(1, int.MaxValue)] int MinimumEnrollment,
    [Required] decimal? TuitionFee);
