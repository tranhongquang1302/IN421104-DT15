using LangCenter.Application.Common;
using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Scheduling;

public record SaveClassSessionCommand(Guid ClassId, Guid RoomId, Guid InstructorId, DateOnly Date,
    TimeOnly StartTime, TimeOnly EndTime, Guid? SessionId = null);

public class SaveClassSessionCommandHandler(ITrainingClassRepository classes, IScheduleResourceRepository resources,
    IClassSessionRepository sessions, IUnitOfWork unitOfWork)
{
    public Task<ClassSessionDto> HandleAsync(SaveClassSessionCommand command, CancellationToken ct = default)
    {
        _ = ScheduleTime.StartOfWeek(command.Date);
        ClassSession candidate;
        try
        {
            candidate = ClassSession.Create(command.ClassId, command.RoomId, command.InstructorId,
                ScheduleTime.ToUtc(command.Date, command.StartTime), ScheduleTime.ToUtc(command.Date, command.EndTime));
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        return unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            // Luôn khóa theo thứ tự lớp, phòng, giảng viên trước khi kiểm tra giao giờ.
            var trainingClass = await classes.GetByIdForUpdateAsync(command.ClassId, transactionCt)
                ?? throw new NotFoundException("Không tìm thấy lớp học.");
            var existing = command.SessionId is Guid sessionId
                ? await sessions.GetByIdAsync(command.ClassId, sessionId, transactionCt)
                    ?? throw new NotFoundException("Không tìm thấy buổi học của lớp.")
                : null;
            var room = await resources.GetRoomForUpdateAsync(command.RoomId, transactionCt)
                ?? throw new NotFoundException("Không tìm thấy phòng học.");
            var instructor = await resources.GetInstructorForUpdateAsync(command.InstructorId, transactionCt)
                ?? throw new NotFoundException("Không tìm thấy giảng viên.");

            var conflict = await sessions.FindConflictAsync(candidate, existing?.Id, transactionCt);
            if (conflict is not null)
            {
                var resource = conflict.RoomId == room.Id ? $"Phòng {room.Code}"
                    : conflict.InstructorId == instructor.Id ? $"Giảng viên {instructor.FullName}"
                    : $"Lớp {trainingClass.Name}";
                var otherClass = await classes.GetByIdAsync(conflict.TrainingClassId, transactionCt);
                var start = ScheduleTime.ToLocal(conflict.StartsAt);
                var end = ScheduleTime.ToLocal(conflict.EndsAt);
                throw new ConflictException($"{resource} đã có buổi {otherClass!.Name} ngày {start:dd/MM/yyyy} lúc {start:HH:mm}–{end:HH:mm}.");
            }

            if (existing is null)
                sessions.Add(candidate);
            else
                existing.Reschedule(room.Id, instructor.Id, candidate.StartsAt, candidate.EndsAt);

            var saved = existing ?? candidate;
            return new ClassSessionDto(saved.Id, trainingClass.Id, trainingClass.Name, room.Id, room.Code,
                room.Name, instructor.Id, instructor.Code, instructor.FullName, saved.StartsAt, saved.EndsAt);
        }, ct);
    }
}
