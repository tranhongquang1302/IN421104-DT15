using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LangCenter.Infrastructure.Repositories;

public class ScheduleResourceRepository(LangCenterDbContext db) : IScheduleResourceRepository
{
    public async Task<IReadOnlyList<Room>> GetRoomsAsync(CancellationToken ct = default) =>
        await db.Rooms.AsNoTracking().OrderBy(room => room.Code).ToListAsync(ct);

    public async Task<IReadOnlyList<Instructor>> GetInstructorsAsync(CancellationToken ct = default) =>
        await db.Instructors.AsNoTracking().OrderBy(instructor => instructor.FullName)
            .ThenBy(instructor => instructor.Code).ToListAsync(ct);

    public async Task<Room?> GetRoomForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A transaction is required to lock the room.");
        if (!db.Database.IsNpgsql())
            return await db.Rooms.FirstOrDefaultAsync(room => room.Id == id, ct);

        var rooms = await db.Rooms.FromSql($"""SELECT * FROM rooms WHERE "Id" = {id} FOR UPDATE""").ToListAsync(ct);
        return rooms.SingleOrDefault();
    }

    public async Task<Instructor?> GetInstructorForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A transaction is required to lock the instructor.");
        if (!db.Database.IsNpgsql())
            return await db.Instructors.FirstOrDefaultAsync(instructor => instructor.Id == id, ct);

        var instructors = await db.Instructors.FromSql($"""SELECT * FROM instructors WHERE "Id" = {id} FOR UPDATE""")
            .ToListAsync(ct);
        return instructors.SingleOrDefault();
    }

    public async Task<bool> TryAddAsync(Room room, CancellationToken ct = default)
    {
        if (await db.Rooms.AnyAsync(existing => existing.Code == room.Code, ct))
            return false;
        return await SaveAsync(room, LangCenterDbContext.RoomCodeIndexName, ct);
    }

    public async Task<bool> TryAddAsync(Instructor instructor, CancellationToken ct = default)
    {
        if (await db.Instructors.AnyAsync(existing => existing.Code == instructor.Code, ct))
            return false;
        return await SaveAsync(instructor, LangCenterDbContext.InstructorCodeIndexName, ct);
    }

    private async Task<bool> SaveAsync(object resource, string indexName, CancellationToken ct)
    {
        var entry = db.Add(resource);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation } error && error.ConstraintName == indexName)
        {
            entry.State = EntityState.Detached;
            return false;
        }
    }
}
