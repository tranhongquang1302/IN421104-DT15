using System.ComponentModel.DataAnnotations;
using LangCenter.Application.Scheduling;

namespace LangCenter.API.Scheduling;

public class ClassSessionInput
{
    [Required(ErrorMessage = "Hãy chọn lớp học.")]
    public Guid? ClassId { get; set; }

    [Required(ErrorMessage = "Hãy chọn phòng học.")]
    public Guid? RoomId { get; set; }

    [Required(ErrorMessage = "Hãy chọn giảng viên.")]
    public Guid? InstructorId { get; set; }

    [Required(ErrorMessage = "Hãy chọn ngày học.")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Hãy nhập giờ bắt đầu.")]
    public TimeOnly? StartTime { get; set; }

    [Required(ErrorMessage = "Hãy nhập giờ kết thúc.")]
    public TimeOnly? EndTime { get; set; }

    public SaveClassSessionCommand ToCommand(Guid? sessionId = null) =>
        new(ClassId!.Value, RoomId!.Value, InstructorId!.Value, Date!.Value, StartTime!.Value, EndTime!.Value, sessionId);
}
