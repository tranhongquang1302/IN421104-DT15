using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LangCenter.Infrastructure.Repositories;

public class StudentRepository(LangCenterDbContext db) : IStudentRepository
{
    public async Task<bool> TryAddAsync(Student student, CancellationToken ct = default)
    {
        if (await db.Students.AnyAsync(existing => existing.Email == student.Email, ct))
            return false;

        var entry = db.Students.Add(student);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: LangCenterDbContext.StudentEmailIndexName
        })
        {
            entry.State = EntityState.Detached;
            return false;
        }
    }

    public Task<Student?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Students.AsNoTracking().FirstOrDefaultAsync(student => student.Id == id, ct);

    public async Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken ct = default) =>
        await db.Students.AsNoTracking()
            .OrderBy(student => student.FullName)
            .ThenBy(student => student.Id)
            .ToListAsync(ct);
}
