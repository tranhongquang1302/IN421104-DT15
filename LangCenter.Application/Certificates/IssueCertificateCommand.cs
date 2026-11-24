using System.Security.Cryptography;
using LangCenter.Application.Common;
using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Certificates;

public record IssueCertificateCommand(Guid EnrollmentId, bool CompletionConfirmed);

public record CertificateDto(Guid Id, Guid EnrollmentId, string LookupCode, DateTime IssuedAt)
{
    public static CertificateDto FromCertificate(Certificate certificate) =>
        new(certificate.Id, certificate.EnrollmentId, certificate.LookupCode, certificate.IssuedAt);
}

public class IssueCertificateCommandHandler(IEnrollmentRepository enrollments, IExamAdmissionRepository admissions,
    ICertificateRepository certificates, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public Task<CertificateDto> HandleAsync(IssueCertificateCommand command, CancellationToken ct = default) =>
        unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var enrollment = await enrollments.GetByIdForUpdateAsync(command.EnrollmentId, transactionCt)
                ?? throw new NotFoundException("Không tìm thấy lượt ghi danh.");
            if (await certificates.ExistsAsync(enrollment.Id, transactionCt))
                throw new ConflictException("Lượt ghi danh đã được cấp chứng chỉ.");

            var admitted = await admissions.ExistsAsync(enrollment.Id, transactionCt);
            var lookupCode = RandomNumberGenerator.GetHexString(Certificate.LookupCodeLength);
            Certificate certificate;
            try
            {
                certificate = Certificate.Create(enrollment, command.CompletionConfirmed, admitted, lookupCode,
                    clock.GetUtcNow().UtcDateTime);
            }
            catch (InvalidOperationException ex)
            {
                throw new BusinessRuleException(ex.Message);
            }

            certificates.Add(certificate);
            return CertificateDto.FromCertificate(certificate);
        }, ct);
}
