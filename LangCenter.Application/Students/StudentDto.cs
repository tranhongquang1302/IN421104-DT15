using LangCenter.Domain.Entities;

namespace LangCenter.Application.Students;

public record StudentDto(Guid Id, string FullName, string Email)
{
    public static StudentDto FromStudent(Student student) => new(student.Id, student.FullName, student.Email);
}
