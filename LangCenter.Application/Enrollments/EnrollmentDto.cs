using LangCenter.Domain.Entities;

namespace LangCenter.Application.Enrollments;

public record EnrollmentDto(Guid Id, Guid StudentId, Guid TrainingClassId, decimal TuitionDue, DateTime EnrolledAt)
{
    public static EnrollmentDto FromEnrollment(Enrollment enrollment) =>
        new(enrollment.Id, enrollment.StudentId, enrollment.TrainingClassId,
            enrollment.TuitionDue, enrollment.EnrolledAt);
}
