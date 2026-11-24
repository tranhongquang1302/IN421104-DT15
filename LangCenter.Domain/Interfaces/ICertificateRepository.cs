using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface ICertificateRepository
{
    Task<bool> ExistsAsync(Guid enrollmentId, CancellationToken ct = default);
    void Add(Certificate certificate);
}
