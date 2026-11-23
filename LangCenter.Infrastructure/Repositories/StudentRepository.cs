using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class StudentRepository(LangCenterDbContext db) : IStudentRepository
{
    public async Task AddAsync(Student student, CancellationToken ct = default)
    {
        db.Students.Add(student);
        await db.SaveChangesAsync(ct);
    }

    public Task<Student?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Students.AsNoTracking().FirstOrDefaultAsync(student => student.Id == id, ct);

    public async Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default) =>
        await db.Students.AsNoTracking()
            .OrderBy(student => student.FullName)
            .ThenBy(student => student.Id)
            .ToListAsync(ct);
}
