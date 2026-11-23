using LangCenter.Application.Auth;
using LangCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace LangCenter.Infrastructure.Auth;

public class AccountRepository(UserManager<AppUser> users, SignInManager<AppUser> signIn) : IAccountRepository
{
    public async Task<StaffAccount?> VerifyCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(email);
        if (user?.Email is not string accountEmail)
            return null;

        ct.ThrowIfCancellationRequested();
        var verified = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (!verified.Succeeded)
            return null;

        ct.ThrowIfCancellationRequested();
        var roles = await users.GetRolesAsync(user);
        return roles.Count == 0 ? null : new StaffAccount(user.Id, accountEmail, roles.ToArray());
    }
}
