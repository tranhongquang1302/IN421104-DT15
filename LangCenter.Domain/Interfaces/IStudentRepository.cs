using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface IStudentRepository
{
    Task AddAsync(Student student, CancellationToken ct = default);
    Task<Student?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default);
}
