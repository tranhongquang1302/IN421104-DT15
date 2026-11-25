using LangCenter.API.Reports;
using LangCenter.API.Security;
using LangCenter.Application.Common;
using LangCenter.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = Policies.ManageTuition)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ReportsController(IClassTuitionReportQuery reports, JsonClassTuitionReportStrategy json,
    CsvClassTuitionReportStrategy csv) : ControllerBase
{
    [HttpGet("classes/{id:guid}/tuition")]
    [ProducesResponseType<ClassTuitionReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClassTuition(Guid id, CancellationToken ct, [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        if (page is null && pageSize is null)
            return await RenderAsync(id, json, ct);

        var paging = PageRequest.Of(page ?? 1, pageSize ?? PageRequest.DefaultPageSize);
        var report = await reports.GetPageAsync(id, paging, ct)
            ?? throw new NotFoundException("Không tìm thấy lớp học.");
        return Ok(report);
    }

    [HttpGet("classes/{id:guid}/tuition.csv")]
    [Produces(CsvClassTuitionReportStrategy.MediaType)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ActionResult> DownloadClassTuition(Guid id, CancellationToken ct) => RenderAsync(id, csv, ct);

    private async Task<ActionResult> RenderAsync(Guid id, IClassTuitionReportStrategy strategy, CancellationToken ct)
    {
        var report = await reports.GetByClassIdAsync(id, ct)
            ?? throw new NotFoundException("Không tìm thấy lớp học.");
        return strategy.Render(report);
    }
}
