using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface ICertificateRepository
{
    Task<Certificate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid enrollmentId, CancellationToken ct = default);
    void Add(Certificate certificate);
}
