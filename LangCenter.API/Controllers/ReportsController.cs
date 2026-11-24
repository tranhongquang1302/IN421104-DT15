using LangCenter.API.Security;
using LangCenter.Application.Common;
using LangCenter.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = Policies.ManageTuition)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ReportsController(IClassTuitionReportQuery reports) : ControllerBase
{
    [HttpGet("classes/{id:guid}/tuition")]
    [ProducesResponseType<ClassTuitionReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassTuitionReportDto>> GetClassTuition(Guid id, CancellationToken ct)
    {
        var report = await reports.GetByClassIdAsync(id, ct)
            ?? throw new NotFoundException("Không tìm thấy lớp học.");
        return Ok(report);
    }
}
