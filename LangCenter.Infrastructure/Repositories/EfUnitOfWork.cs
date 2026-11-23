using System.Data;
using LangCenter.Domain.Interfaces;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class EfUnitOfWork(LangCenterDbContext db) : IUnitOfWork
{
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation, CancellationToken ct = default)
    {
        await using var transaction = db.Database.IsNpgsql()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct)
            : await db.Database.BeginTransactionAsync(ct);

        var result = await operation(ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }
}
