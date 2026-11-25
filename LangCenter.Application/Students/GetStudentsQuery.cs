using LangCenter.Application.Common;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Students;

public class GetStudentsQueryHandler(IStudentRepository students)
{
    public async Task<IReadOnlyList<StudentDto>> HandleAsync(CancellationToken ct = default)
    {
        var results = await students.GetAllAsync(ct);
        return results.Select(StudentDto.FromStudent).ToArray();
    }

    public async Task<PagedResult<StudentDto>> HandleAsync(PageRequest paging, CancellationToken ct = default)
    {
        var (items, totalCount) = await students.GetPageAsync(paging.Skip, paging.PageSize, ct);
        return new PagedResult<StudentDto>(
            items.Select(StudentDto.FromStudent).ToArray(), totalCount, paging.Page, paging.PageSize);
    }
}

public record GetStudentByIdQuery(Guid Id);

public class GetStudentByIdQueryHandler(IStudentRepository students)
{
    public async Task<StudentDto> HandleAsync(GetStudentByIdQuery query, CancellationToken ct = default)
    {
        var student = await students.GetByIdAsync(query.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy học viên.");
        return StudentDto.FromStudent(student);
    }
}
