namespace LangCenter.Domain.Entities;

public class Instructor
{
    public const int MaxCodeLength = 20;
    public const int MaxFullNameLength = 120;

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;

    private Instructor() { }

    public static Instructor Create(string code, string fullName)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > MaxCodeLength)
            throw new ArgumentException($"Mã giảng viên phải có từ 1 đến {MaxCodeLength} ký tự.", nameof(code));
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > MaxFullNameLength)
            throw new ArgumentException($"Họ tên giảng viên phải có từ 1 đến {MaxFullNameLength} ký tự.", nameof(fullName));

        return new Instructor { Id = Guid.NewGuid(), Code = code.Trim().ToUpperInvariant(), FullName = fullName.Trim() };
    }
}
