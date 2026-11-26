namespace LangCenter.Domain.Entities;

public class Room
{
    public const int MaxCodeLength = 20;
    public const int MaxNameLength = 100;

    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private Room() { }

    public static Room Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > MaxCodeLength)
            throw new ArgumentException($"Mã phòng phải có từ 1 đến {MaxCodeLength} ký tự.", nameof(code));
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
            throw new ArgumentException($"Tên phòng phải có từ 1 đến {MaxNameLength} ký tự.", nameof(name));

        return new Room { Id = Guid.NewGuid(), Code = code.Trim().ToUpperInvariant(), Name = name.Trim() };
    }
}
