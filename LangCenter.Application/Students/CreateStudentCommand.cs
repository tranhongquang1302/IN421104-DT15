using LangCenter.Application.Common;
using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Students;

public record CreateStudentCommand(string FullName, string Email);

public class CreateStudentCommandHandler(IStudentRepository students)
{
    public async Task<StudentDto> HandleAsync(CreateStudentCommand command, CancellationToken ct = default)
    {
        Student student;
        try
        {
            student = Student.Create(command.FullName, command.Email);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        if (!await students.TryAddAsync(student, ct))
            throw new ConflictException("Email học viên đã được sử dụng.");

        return StudentDto.FromStudent(student);
    }
}
