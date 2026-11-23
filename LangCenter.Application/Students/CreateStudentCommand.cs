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

        await students.AddAsync(student, ct);
        return StudentDto.FromStudent(student);
    }
}
