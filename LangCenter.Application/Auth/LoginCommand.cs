namespace LangCenter.Application.Auth;

public record LoginCommand(string Email, string Password);

public record LoginResult(string AccessToken, int ExpiresIn, string TokenType = "Bearer");

public record StaffAccount(Guid Id, string Email, IReadOnlyList<string> Roles);

public interface IAccountRepository
{
    Task<StaffAccount?> VerifyCredentialsAsync(string email, string password, CancellationToken ct = default);
}

public interface ITokenService
{
    LoginResult IssueAccessToken(StaffAccount account);
}

public class LoginCommandHandler(IAccountRepository accounts, ITokenService tokens)
{
    public async Task<LoginResult?> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var account = await accounts.VerifyCredentialsAsync(command.Email.Trim(), command.Password, ct);
        return account is null ? null : tokens.IssueAccessToken(account);
    }
}
