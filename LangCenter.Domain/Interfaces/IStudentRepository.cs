using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface IStudentRepository
{
    Task<bool> TryAddAsync(Student student, CancellationToken ct = default);
    Task<Student?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<Student> Items, int TotalCount)> GetPageAsync(int skip, int take, CancellationToken ct = default);
}
