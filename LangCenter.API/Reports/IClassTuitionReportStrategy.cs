using LangCenter.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Reports;

public interface IClassTuitionReportStrategy
{
    ActionResult Render(ClassTuitionReportDto report);
}
