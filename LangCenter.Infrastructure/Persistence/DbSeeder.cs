using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LangCenter.Infrastructure.Persistence;

public class DbSeeder(UserManager<AppUser> users, RoleManager<IdentityRole<Guid>> roles)
{
    public async Task SeedDevelopmentAccountsAsync(string password)
    {
        await SeedAccountAsync("admin@langcenter.local", Roles.Admin, password);
        await SeedAccountAsync("registrar@langcenter.local", Roles.Registrar, password);
        await SeedAccountAsync("cashier@langcenter.local", Roles.Cashier, password);
    }

    private async Task SeedAccountAsync(string email, string role, string password)
    {
        if (!await roles.RoleExistsAsync(role))
            EnsureSucceeded(await roles.CreateAsync(new IdentityRole<Guid>(role)));

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email };
            EnsureSucceeded(await users.CreateAsync(user, password));
        }

        if (!await users.IsInRoleAsync(user, role))
            EnsureSucceeded(await users.AddToRoleAsync(user, role));
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Không thể tạo tài khoản phát triển. {string.Join(", ", result.Errors.Select(error => error.Code))}");
    }
}
