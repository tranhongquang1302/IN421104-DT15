namespace LangCenter.Application.Certificates;

public record CertificateVerificationDto(string StudentName, string ClassName, DateTime IssuedAt);

public interface ICertificateVerificationQuery
{
    Task<CertificateVerificationDto?> FindByLookupCodeAsync(string lookupCode, CancellationToken ct = default);
}
