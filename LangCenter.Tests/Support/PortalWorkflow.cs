using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace LangCenter.Tests.Support;

public static class PortalWorkflow
{
    public static async Task ExerciseAsync(HttpClient registrar, HttpClient cashier, HttpClient visitor)
    {
        using var blockedStudents = await cashier.GetAsync("/workspace/students");
        Assert.Contains("/forbidden", blockedStudents.Headers.Location?.OriginalString);
        using var blockedTuition = await registrar.GetAsync("/workspace/tuition");
        Assert.Contains("/forbidden", blockedTuition.Headers.Location?.OriginalString);
        var cashierHome = await PortalForms.GetPageAsync(cashier, "/workspace");
        Assert.DoesNotContain("href=\"/workspace/classes\"", cashierHome);
        Assert.Contains("Thu ngân", cashierHome);

        const string studentName = "Portal Student";
        const string studentEmail = "portal.student@example.test";
        using var student = await PortalForms.PostAsync(registrar, "/workspace/students",
            await PortalForms.GetPageAsync(registrar, "/workspace/students"), new Dictionary<string, string>
            {
                ["Input.FullName"] = studentName,
                ["Input.Email"] = studentEmail
            });
        Assert.Equal(HttpStatusCode.Redirect, student.StatusCode);

        using var createdClass = await PortalForms.PostAsync(registrar, "/workspace/classes",
            await PortalForms.GetPageAsync(registrar, "/workspace/classes"), new Dictionary<string, string>
            {
                ["Input.Name"] = "Portal English A1",
                ["Input.MinimumEnrollment"] = "1",
                ["Input.TuitionFee"] = "1000"
            });
        Assert.Equal(HttpStatusCode.Redirect, createdClass.StatusCode);
        var classPath = createdClass.Headers.Location!.OriginalString;
        var classId = classPath.Split('/').Last();
        var classPage = await PortalForms.GetPageAsync(registrar, classPath);
        using var earlyLaunch = await PortalForms.PostAsync(registrar, $"{classPath}?handler=Launch", classPage);
        Assert.Contains("Lớp chưa đủ sĩ số", await PortalForms.ReadHtmlAsync(earlyLaunch));

        var studentId = MatchValue(classPage, "value=\"([0-9a-f-]{36})\">Portal Student");
        using var enrolled = await PortalForms.PostAsync(registrar, $"{classPath}?handler=Enroll", classPage,
            new Dictionary<string, string> { ["StudentId"] = studentId });
        Assert.Equal(HttpStatusCode.Redirect, enrolled.StatusCode);

        var roster = await PortalForms.GetPageAsync(registrar, classPath);
        var enrollmentPath = MatchValue(roster, "href=\"(/workspace/enrollments/[0-9a-f-]{36})\"");
        using var launched = await PortalForms.PostAsync(registrar, $"{classPath}?handler=Launch", roster);
        Assert.Equal(HttpStatusCode.Redirect, launched.StatusCode);

        var registrarEnrollment = await PortalForms.GetPageAsync(registrar, enrollmentPath);
        using var unpaidExam = await PortalForms.PostAsync(registrar, $"{enrollmentPath}?handler=Exam", registrarEnrollment);
        Assert.Contains("còn nợ học phí", await PortalForms.ReadHtmlAsync(unpaidExam));
        using var registrarPayment = await PortalForms.PostAsync(registrar, $"{enrollmentPath}?handler=Payment", registrarEnrollment,
            new Dictionary<string, string> { ["amount"] = "1000" });
        Assert.Contains("/forbidden", registrarPayment.Headers.Location?.OriginalString);

        var search = await PortalForms.GetPageAsync(cashier, $"/workspace/tuition?email={Uri.EscapeDataString(studentEmail)}");
        Assert.Contains(enrollmentPath, search);
        var cashierEnrollment = await PortalForms.GetPageAsync(cashier, enrollmentPath);
        Assert.DoesNotContain("Xác nhận dự thi", cashierEnrollment);
        using var cashierExam = await PortalForms.PostAsync(cashier, $"{enrollmentPath}?handler=Exam", cashierEnrollment);
        Assert.Contains("/forbidden", cashierExam.Headers.Location?.OriginalString);
        var requestId = MatchValue(cashierEnrollment, "name=\"requestId\" value=\"([0-9a-f-]{36})\"");
        using var paid = await PortalForms.PostAsync(cashier, $"{enrollmentPath}?handler=Payment", cashierEnrollment,
            new Dictionary<string, string> { ["amount"] = "1000", ["requestId"] = requestId });
        Assert.Equal(HttpStatusCode.Redirect, paid.StatusCode);
        var report = await PortalForms.GetPageAsync(cashier, $"/workspace/tuition?classId={classId}");
        Assert.Contains("Portal English A1", report);
        Assert.Contains("Portal Student", report);
        Assert.Contains("1.000 đ", report);

        registrarEnrollment = await PortalForms.GetPageAsync(registrar, enrollmentPath);
        using var exam = await PortalForms.PostAsync(registrar, $"{enrollmentPath}?handler=Exam", registrarEnrollment);
        Assert.Equal(HttpStatusCode.Redirect, exam.StatusCode);
        var admitted = await PortalForms.GetPageAsync(registrar, enrollmentPath);
        using var issued = await PortalForms.PostAsync(registrar, $"{enrollmentPath}?handler=Certificate", admitted,
            new Dictionary<string, string> { ["completionConfirmed"] = "true" });
        Assert.Equal(HttpStatusCode.Redirect, issued.StatusCode);

        var certificate = await PortalForms.GetPageAsync(registrar, enrollmentPath);
        var certificatePath = MatchValue(certificate, "href=\"https?://[^/\"]+(/certificates/verify/[0-9A-F]{32})\"");
        var verified = await PortalForms.GetPageAsync(visitor, certificatePath);
        Assert.Contains(studentName, verified);
        Assert.Contains("Portal English A1", verified);
        Assert.DoesNotContain(studentEmail, verified);
        using var invalidCertificate = await visitor.GetAsync("/certificates/verify/00000000000000000000000000000000");
        Assert.Equal(HttpStatusCode.NotFound, invalidCertificate.StatusCode);

        using var qr = await registrar.GetAsync($"{enrollmentPath}?handler=Qr");
        Assert.Equal("image/png", qr.Content.Headers.ContentType?.MediaType);
        var image = await qr.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 137, 80, 78, 71 }, image[..4]);
        using var blockedQr = await cashier.GetAsync($"{enrollmentPath}?handler=Qr");
        Assert.Contains("/forbidden", blockedQr.Headers.Location?.OriginalString);
    }

    private static string MatchValue(string html, string pattern)
    {
        var match = Regex.Match(html, pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        Assert.True(match.Success, $"Expected a portal workflow value matching {pattern}.");
        return match.Groups[1].Value;
    }
}
