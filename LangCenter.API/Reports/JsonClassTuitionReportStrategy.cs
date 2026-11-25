using LangCenter.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Reports;

public class JsonClassTuitionReportStrategy : IClassTuitionReportStrategy
{
    public ActionResult Render(ClassTuitionReportDto report) => new OkObjectResult(report);
}
