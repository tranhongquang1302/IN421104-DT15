using LangCenter.API.Certificates;
using LangCenter.API.Security;
using LangCenter.Application.Certificates;
using LangCenter.Application.Enrollments;
using LangCenter.Application.Exams;
using LangCenter.Application.Tuition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Pages.Workspace;

[Authorize(Policy = Policies.ReadTuitionBalance)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class EnrollmentDetailsModel(IEnrollmentWorkspaceQuery enrollments, GetTuitionBalanceQueryHandler balance,
    RecordPaymentCommandHandler recordPayment, ConfirmExamAdmissionCommandHandler confirmExam,
    IssueCertificateCommandHandler issueCertificate, IAuthorizationService authorization,
    CertificateLinks links, IQrCodeRenderer qrCodes) : PortalPageModel
{
    public EnrollmentWorkspaceDto Enrollment { get; private set; } = null!;
    public TuitionBalanceDto Balance { get; private set; } = null!;
    public bool CanCollect { get; private set; }
    public bool CanConfirmExam { get; private set; }
    public bool CanIssueCertificate { get; private set; }
    public decimal? Amount { get; private set; }
    public string? RecordedPaymentAmount { get; private set; }
    public Guid RequestId { get; private set; } = Guid.NewGuid();

    public Uri? VerificationUrl => CanIssueCertificate && Enrollment.Certificate is { } certificate
        ? links.GetPublicVerificationUrl(certificate.LookupCode) : null;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        var page = await ShowAsync(id, ct);
        if (CanCollect)
            RecordedPaymentAmount = TempData[PaymentAmountKey(id)] as string;

        return page;
    }

    public async Task<IActionResult> OnPostPaymentAsync(Guid id, decimal? amount, Guid? requestId, CancellationToken ct)
    {
        if (!await HasPermissionAsync(Policies.ManageTuition))
            return Forbid(WebSession.Scheme);

        Amount = amount;
        if (requestId.HasValue)
            RequestId = requestId.Value;
        if (amount is null)
            ModelState.AddModelError(string.Empty, "Hãy nhập số tiền cần thu.");

        PaymentDto? payment = null;
        if (!await TrySaveAsync(async () =>
                payment = await recordPayment.HandleAsync(new RecordPaymentCommand(id, amount!.Value, RequestId), ct)))
        {
            return await ShowAsync(id, ct);
        }

        TempData[PaymentAmountKey(id)] = PortalDisplay.Money(payment!.Amount);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostExamAsync(Guid id, CancellationToken ct)
    {
        if (!await HasPermissionAsync(Policies.ConfirmExamAdmission))
            return Forbid(WebSession.Scheme);

        if (!await TrySaveAsync(() => confirmExam.HandleAsync(new ConfirmExamAdmissionCommand(id), ct)))
            return await ShowAsync(id, ct);

        TempData["StatusMessage"] = "Đã xác nhận học viên đủ điều kiện dự thi.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCertificateAsync(Guid id, bool completionConfirmed, CancellationToken ct)
    {
        if (!await HasPermissionAsync(Policies.ManageCertificates))
            return Forbid(WebSession.Scheme);

        if (!await TrySaveAsync(() => issueCertificate.HandleAsync(new IssueCertificateCommand(id, completionConfirmed), ct)))
            return await ShowAsync(id, ct);

        TempData["StatusMessage"] = "Đã cấp chứng chỉ.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnGetQrAsync(Guid id, CancellationToken ct)
    {
        if (!await HasPermissionAsync(Policies.ManageCertificates))
            return Forbid(WebSession.Scheme);

        var enrollment = await enrollments.GetDetailsAsync(id, ct);
        if (enrollment?.Certificate is not { } certificate)
            return NotFound();

        return File(qrCodes.RenderPng(links.GetPublicVerificationUrl(certificate.LookupCode).AbsoluteUri), "image/png");
    }

    private async Task<IActionResult> ShowAsync(Guid id, CancellationToken ct)
    {
        var enrollment = await enrollments.GetDetailsAsync(id, ct);
        if (enrollment is null)
            return NotFound();

        Enrollment = enrollment;
        Balance = await balance.HandleAsync(new GetTuitionBalanceQuery(id), ct);
        CanCollect = await HasPermissionAsync(Policies.ManageTuition);
        CanConfirmExam = await HasPermissionAsync(Policies.ConfirmExamAdmission);
        CanIssueCertificate = await HasPermissionAsync(Policies.ManageCertificates);
        return Page();
    }

    private async Task<bool> HasPermissionAsync(string policy) => (await authorization.AuthorizeAsync(User, policy)).Succeeded;

    private static string PaymentAmountKey(Guid id) => $"RecordedPaymentAmount:{id}";
}
