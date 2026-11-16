using Microsoft.AspNetCore.Identity;
using ProofTrail.Models;
namespace ProofTrail.Data;
public static class DbInitializer
{
    // Only creates the three roles. No account (including Admin) is created in code:
    // the Admin is created once through the first-run setup page (Identity/Account/SetupAdmin).
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        var database = services.GetRequiredService<SqlDatabase>();
        await database.BeginAsync();
        try {
            foreach (var name in new[] { Roles.Admin, Roles.Issuer, Roles.Holder })
                if (!await roles.RoleExistsAsync(name)) Check(await roles.CreateAsync(new IdentityRole(name)));
            await database.CommitAsync();
        }
        catch { await database.RollbackAsync(); throw; }
    }
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
