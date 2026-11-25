using System.Text;
using LangCenter.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Reports;

public class CsvClassTuitionReportStrategy : IClassTuitionReportStrategy
{
    public const string MediaType = "text/csv";

    public ActionResult Render(ClassTuitionReportDto report)
    {
        // UTF-8 BOM giúp Excel nhận đúng tiếng Việt.
        var csv = new StringBuilder("\uFEFFLớp,Học viên,Phải thu (đồng),Đã thu (đồng),Còn nợ (đồng)\r\n");
        foreach (var row in report.Enrollments)
            AppendRow(csv, report.ClassName, row.StudentName, row.TuitionDue, row.PaidAmount, row.OutstandingAmount);

        AppendRow(csv, report.ClassName, "Tổng cộng", report.TuitionDue, report.PaidAmount, report.OutstandingAmount);
        return new FileContentResult(Encoding.UTF8.GetBytes(csv.ToString()), $"{MediaType}; charset=utf-8")
        {
            FileDownloadName = $"tuition-{report.TrainingClassId:N}.csv"
        };
    }

    private static void AppendRow(StringBuilder csv, string className, string label, decimal due, decimal paid,
        decimal outstanding)
    {
        csv.Append(Quote(className)).Append(',').Append(Quote(label))
            .Append(FormattableString.Invariant($",{due:0},{paid:0},{outstanding:0}\r\n"));
    }

    private static string Quote(string value)
    {
        var trimmed = value.TrimStart();
        // Giữ tên lớp và tên học viên ở dạng văn bản khi mở bằng bảng tính.
        if ((value.Length > 0 && char.IsControl(value[0])) ||
            (trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@' or '＝' or '＋' or '－' or '＠'))
        {
            value = "'" + value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
