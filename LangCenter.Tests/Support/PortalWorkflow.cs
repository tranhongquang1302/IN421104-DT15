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
        Assert.DoesNotContain("Thêm học viên", cashierHome);
        Assert.DoesNotContain("Tạo lớp", cashierHome);
        Assert.Contains("Thu học phí", cashierHome);
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
        var dashboard = await PortalForms.GetPageAsync(registrar, "/workspace");
        Assert.Contains("Thêm học viên", dashboard);
        Assert.DoesNotContain("Thu học phí", dashboard);
        Assert.Contains(">0 / 1</strong>", dashboard);
        Assert.Contains("Thiếu 1 học viên", dashboard);
        Assert.Contains($"href=\"{classPath}#enroll-student\"", dashboard);
        cashierHome = await PortalForms.GetPageAsync(cashier, "/workspace");
        Assert.Contains("Thiếu 1 học viên", cashierHome);
        Assert.DoesNotContain("#enroll-student", cashierHome);

        var classPage = await PortalForms.GetPageAsync(registrar, classPath);
        using var earlyLaunch = await PortalForms.PostAsync(registrar, $"{classPath}?handler=Launch", classPage);
        var launchBlocked = await PortalForms.ReadHtmlAsync(earlyLaunch);
        Assert.Contains("Lớp chưa đủ sĩ số", launchBlocked);
        Assert.Contains("Lớp cần thêm 1 học viên để khai giảng.", launchBlocked);
        Assert.Contains("href=\"#enroll-student\"", launchBlocked);

        var studentId = MatchValue(classPage, "value=\"([0-9a-f-]{36})\">Portal Student");
        using var enrolled = await PortalForms.PostAsync(registrar, $"{classPath}?handler=Enroll", classPage,
            new Dictionary<string, string> { ["StudentId"] = studentId });
        Assert.Equal(HttpStatusCode.Redirect, enrolled.StatusCode);

        var readyDashboard = await PortalForms.GetPageAsync(registrar, "/workspace");
        Assert.Contains("Đủ sĩ số", readyDashboard);
        Assert.DoesNotContain($"href=\"{classPath}#enroll-student\"", readyDashboard);
        var roster = await PortalForms.GetPageAsync(registrar, classPath);
        var enrollmentPath = MatchValue(roster, "href=\"(/workspace/enrollments/[0-9a-f-]{36})\"");
        using var launched = await PortalForms.PostAsync(registrar, $"{classPath}?handler=Launch", roster);
        Assert.Equal(HttpStatusCode.Redirect, launched.StatusCode);
        var launchedClass = await PortalForms.GetPageAsync(registrar, classPath);
        Assert.Contains("Đã khai giảng lớp Portal English A1.", launchedClass);
        dashboard = await PortalForms.GetPageAsync(registrar, "/workspace");
        Assert.Contains("Portal English A1", dashboard);
        Assert.Contains(classPath, dashboard);
        Assert.Contains(">1 / 1</strong>", dashboard);

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
        const string paymentRequestId = "name=\"requestId\" value=\"([0-9a-f-]{36})\"";
        var paymentPath = $"{enrollmentPath}?handler=Payment";
        var firstPayment = new Dictionary<string, string>
        {
            ["amount"] = "400",
            ["requestId"] = MatchValue(cashierEnrollment, paymentRequestId)
        };
        using var partialPayment = await PortalForms.PostAsync(cashier, paymentPath, cashierEnrollment, firstPayment);
        Assert.Equal(HttpStatusCode.Redirect, partialPayment.StatusCode);
        var paymentFeedback = await PortalForms.GetPageAsync(cashier, enrollmentPath);
        Assert.Contains("Đã ghi nhận khoản thanh toán", paymentFeedback);
        Assert.Contains("Số tiền ghi nhận <strong>400 đ</strong> · Còn nợ <strong>600 đ</strong>", paymentFeedback);
        var refreshedBalance = await PortalForms.GetPageAsync(cashier, enrollmentPath);
        Assert.DoesNotContain("Đã ghi nhận khoản thanh toán", refreshedBalance);

        using var repeatedPayment = await PortalForms.PostAsync(cashier, paymentPath, cashierEnrollment, firstPayment);
        Assert.Equal(HttpStatusCode.Redirect, repeatedPayment.StatusCode);
        var repeatedFeedback = await PortalForms.GetPageAsync(cashier, enrollmentPath);
        Assert.Contains("Số tiền ghi nhận <strong>400 đ</strong> · Còn nợ <strong>600 đ</strong>", repeatedFeedback);

        var finalRequestId = MatchValue(repeatedFeedback, paymentRequestId);
        using var overpayment = await PortalForms.PostAsync(cashier, paymentPath, repeatedFeedback,
            new Dictionary<string, string> { ["amount"] = "601", ["requestId"] = finalRequestId });
        var paymentBlocked = await PortalForms.ReadHtmlAsync(overpayment);
        Assert.Contains("Khoản thanh toán vượt quá học phí còn nợ.", paymentBlocked);
        Assert.Contains("Có thể thu tối đa 600 đ.", paymentBlocked);
        Assert.DoesNotContain("Đã ghi nhận khoản thanh toán", paymentBlocked);

        using var paid = await PortalForms.PostAsync(cashier, paymentPath, paymentBlocked,
            new Dictionary<string, string> { ["amount"] = "600", ["requestId"] = finalRequestId });
        Assert.Equal(HttpStatusCode.Redirect, paid.StatusCode);
        var completePayment = await PortalForms.GetPageAsync(cashier, enrollmentPath);
        Assert.Contains("Số tiền ghi nhận <strong>600 đ</strong> · Còn nợ <strong>0 đ</strong>", completePayment);
        var report = await PortalForms.GetPageAsync(cashier, $"/workspace/tuition?classId={classId}");
        Assert.Contains("Portal English A1", report);
        Assert.Contains("Portal Student", report);
        Assert.Contains("1.000 đ", report);
        var downloadPath = MatchValue(report, "href=\"(/workspace/tuition\\?[^\"\\s]*handler=Download[^\"\\s]*)\"");
        Assert.Contains($"classId={classId}", downloadPath);
        using var download = await cashier.GetAsync(downloadPath);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("text/csv", download.Content.Headers.ContentType?.MediaType);
        Assert.Contains("\"Portal Student\",1000,1000,0", await download.Content.ReadAsStringAsync());
        using var blockedDownload = await registrar.GetAsync(downloadPath);
        Assert.Contains("/forbidden", blockedDownload.Headers.Location?.OriginalString);

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
