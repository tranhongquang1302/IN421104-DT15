using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface IClassSessionRepository
{
    Task<ClassSession?> GetByIdAsync(Guid classId, Guid id, CancellationToken ct = default);
    Task<ClassSession?> FindConflictAsync(ClassSession candidate, Guid? excludedId, CancellationToken ct = default);
    void Add(ClassSession session);
}
