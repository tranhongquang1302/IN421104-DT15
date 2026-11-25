using System.Text;
using LangCenter.API.Reports;
using LangCenter.Application.Reports;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LangCenter.Tests.Presentation;

public class ClassTuitionReportStrategyTests
{
    [Fact]
    public void Render_SameReport_ReturnsJsonAndCsvWithMatchingTotals()
    {
        // Arrange
        var classId = Guid.NewGuid();
        var report = new ClassTuitionReportDto(classId, "Tiếng Anh, \"A1\"",
            [new(Guid.NewGuid(), "Nguyễn Minh An", 1_200_000m, 400_000m)]);
        IClassTuitionReportStrategy json = new JsonClassTuitionReportStrategy();
        IClassTuitionReportStrategy csv = new CsvClassTuitionReportStrategy();

        // Act
        var jsonResult = json.Render(report);
        var csvResult = csv.Render(report);

        // Assert
        Assert.Same(report, Assert.IsType<OkObjectResult>(jsonResult).Value);
        var file = Assert.IsType<FileContentResult>(csvResult);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.Equal($"tuition-{classId:N}.csv", file.FileDownloadName);
        var content = Encoding.UTF8.GetString(file.FileContents);
        Assert.StartsWith("\uFEFFLớp,Học viên,Phải thu (đồng),Đã thu (đồng),Còn nợ (đồng)\r\n", content);
        Assert.Contains("\"Tiếng Anh, \"\"A1\"\"\",\"Nguyễn Minh An\",1200000,400000,800000\r\n", content);
        Assert.EndsWith("\"Tiếng Anh, \"\"A1\"\"\",\"Tổng cộng\",1200000,400000,800000\r\n", content);
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("  +1", "'  +1")]
    [InlineData("\tTên học viên", "'\tTên học viên")]
    [InlineData("Họ\r\nTên", "Họ\r\nTên")]
    public void Render_WithSpecialText_PreservesSafeQuotedCell(string studentName, string expected)
    {
        // Arrange
        var report = new ClassTuitionReportDto(Guid.NewGuid(), "Tiếng Nhật N5",
            [new(Guid.NewGuid(), studentName, 1000m, 0m)]);
        var strategy = new CsvClassTuitionReportStrategy();

        // Act
        var result = strategy.Render(report);

        // Assert
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Contains($"\"Tiếng Nhật N5\",\"{expected}\",1000,0,1000\r\n", Encoding.UTF8.GetString(file.FileContents));
    }
}
