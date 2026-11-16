using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using ProofTrail.Data;
using ProofTrail.Models;
namespace ProofTrail.IdentityStores;

// Identity still hashes passwords, validates accounts, creates cookies and enforces lockout.
// This class only supplies SQL storage; it does not implement authentication itself.
public sealed class DapperUserStore(SqlDatabase database) :
    IUserPasswordStore<ApplicationUser>, IUserEmailStore<ApplicationUser>,
    IUserRoleStore<ApplicationUser>, IUserSecurityStampStore<ApplicationUser>,
    IUserLockoutStore<ApplicationUser>
{
    private readonly IdentityErrorDescriber _errors = new();
    private static readonly string Columns = "Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FullName, CreatedAt";
    public void Dispose() { } // The DI scope owns SqlDatabase.
    private CommandDefinition Command(string sql, object? args, CancellationToken ct) =>
        new(sql, args, database.Transaction, cancellationToken: ct);
    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct)
    {
        try {
            await (await database.OpenAsync(ct)).ExecuteAsync(Command($"""
                INSERT INTO dbo.AspNetUsers ({Columns}) VALUES
                (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed,
                 @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @PhoneNumber, @PhoneNumberConfirmed,
                 @TwoFactorEnabled, @LockoutEnd, @LockoutEnabled, @AccessFailedCount, @FullName, @CreatedAt)
                """, user, ct));
            return IdentityResult.Success;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) {
            return IdentityResult.Failed(new IdentityError { Code = "DuplicateAccount", Description = "An account with this email or username already exists." });
        }
    }
    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct)
    {
        var oldStamp = user.ConcurrencyStamp;
        var newStamp = Guid.NewGuid().ToString();
        var args = new DynamicParameters(user);
        args.Add("OldStamp", oldStamp); args.Add("NewStamp", newStamp);
        try {
            var count = await (await database.OpenAsync(ct)).ExecuteAsync(Command("""
                UPDATE dbo.AspNetUsers SET UserName=@UserName, NormalizedUserName=@NormalizedUserName,
                    Email=@Email, NormalizedEmail=@NormalizedEmail, EmailConfirmed=@EmailConfirmed,
                    PasswordHash=@PasswordHash, SecurityStamp=@SecurityStamp, ConcurrencyStamp=@NewStamp,
                    PhoneNumber=@PhoneNumber, PhoneNumberConfirmed=@PhoneNumberConfirmed,
                    TwoFactorEnabled=@TwoFactorEnabled, LockoutEnd=@LockoutEnd, LockoutEnabled=@LockoutEnabled,
                    AccessFailedCount=@AccessFailedCount, FullName=@FullName
                WHERE Id=@Id AND (ConcurrencyStamp=@OldStamp OR (ConcurrencyStamp IS NULL AND @OldStamp IS NULL))
                """, args, ct));
            if (count != 1) return IdentityResult.Failed(_errors.ConcurrencyFailure());
            user.ConcurrencyStamp = newStamp;
            return IdentityResult.Success;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) {
            return IdentityResult.Failed(new IdentityError { Code = "DuplicateAccount", Description = "An account with this email or username already exists." });
        }
    }
    public async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct)
    {
        // No account-delete UI is exposed. Referenced users are protected by foreign keys.
        var ownsTransaction = database.Transaction == null;
        if (ownsTransaction) await database.BeginAsync();
        try {
            var connection = await database.OpenAsync(ct);
            var stamp = await connection.QuerySingleOrDefaultAsync<ApplicationUser>(Command(
                "SELECT Id, ConcurrencyStamp FROM dbo.AspNetUsers WITH (UPDLOCK, HOLDLOCK) WHERE Id=@Id", user, ct));
            if (stamp == null || stamp.ConcurrencyStamp != user.ConcurrencyStamp) {
                if (ownsTransaction) await database.RollbackAsync();
                return IdentityResult.Failed(_errors.ConcurrencyFailure());
            }
            await connection.ExecuteAsync(Command("DELETE FROM dbo.AspNetUserRoles WHERE UserId=@Id", user, ct));
            await connection.ExecuteAsync(Command("DELETE FROM dbo.AspNetUsers WHERE Id=@Id", user, ct));
            if (ownsTransaction) await database.CommitAsync();
            return IdentityResult.Success;
        }
        catch {
            if (ownsTransaction) await database.RollbackAsync();
            throw;
        }
    }
    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct) =>
        await (await database.OpenAsync(ct)).QuerySingleOrDefaultAsync<ApplicationUser>(Command(
            $"SELECT {Columns} FROM dbo.AspNetUsers WHERE Id=@userId", new { userId }, ct));
    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct) =>
        await (await database.OpenAsync(ct)).QuerySingleOrDefaultAsync<ApplicationUser>(Command(
            $"SELECT {Columns} FROM dbo.AspNetUsers WHERE NormalizedUserName=@normalizedUserName", new { normalizedUserName }, ct));
    public async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        await (await database.OpenAsync(ct)).QuerySingleOrDefaultAsync<ApplicationUser>(Command(
            $"SELECT {Columns} FROM dbo.AspNetUsers WHERE NormalizedEmail=@normalizedEmail", new { normalizedEmail }, ct));
    public async Task AddToRoleAsync(ApplicationUser user, string normalizedRoleName, CancellationToken ct)
    {
        var connection = await database.OpenAsync(ct);
        var roleId = await connection.QuerySingleAsync<string>(Command(
            "SELECT Id FROM dbo.AspNetRoles WHERE NormalizedName=@normalizedRoleName", new { normalizedRoleName }, ct));
        await connection.ExecuteAsync(Command("""
            INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
            SELECT @UserId, @roleId WHERE NOT EXISTS
            (SELECT 1 FROM dbo.AspNetUserRoles WHERE UserId=@UserId AND RoleId=@roleId)
            """, new { UserId = user.Id, roleId }, ct));
    }
    public async Task RemoveFromRoleAsync(ApplicationUser user, string normalizedRoleName, CancellationToken ct) =>
        await (await database.OpenAsync(ct)).ExecuteAsync(Command("""
            DELETE ur FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId
            WHERE ur.UserId=@Id AND r.NormalizedName=@normalizedRoleName
            """, new { user.Id, normalizedRoleName }, ct));
    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken ct) =>
        (await (await database.OpenAsync(ct)).QueryAsync<string>(Command("""
            SELECT r.Name FROM dbo.AspNetRoles r JOIN dbo.AspNetUserRoles ur ON ur.RoleId=r.Id WHERE ur.UserId=@Id
            """, new { user.Id }, ct))).ToList();
    public async Task<bool> IsInRoleAsync(ApplicationUser user, string normalizedRoleName, CancellationToken ct) =>
        await (await database.OpenAsync(ct)).ExecuteScalarAsync<int>(Command("""
            SELECT COUNT(*) FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId
            WHERE ur.UserId=@Id AND r.NormalizedName=@normalizedRoleName
            """, new { user.Id, normalizedRoleName }, ct)) > 0;
    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken ct) =>
        (await (await database.OpenAsync(ct)).QueryAsync<ApplicationUser>(Command("""
            SELECT u.* FROM dbo.AspNetUsers u JOIN dbo.AspNetUserRoles ur ON ur.UserId=u.Id
            JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE r.NormalizedName=@normalizedRoleName
            """, new { normalizedRoleName }, ct))).ToList();
    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.UserName); }
    public Task SetUserNameAsync(ApplicationUser user, string? value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); user.UserName = value; return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.NormalizedUserName); }
    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); user.NormalizedUserName = value; return Task.CompletedTask; }
    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.PasswordHash); }
    public Task SetPasswordHashAsync(ApplicationUser user, string? value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); user.PasswordHash = value; return Task.CompletedTask; }
    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.Email); }
    public Task SetEmailAsync(ApplicationUser user, string? value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); user.Email = value; return Task.CompletedTask; }
    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.NormalizedEmail); }
    public Task SetNormalizedEmailAsync(ApplicationUser user, string? value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); user.NormalizedEmail = value; return Task.CompletedTask; }
    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.EmailConfirmed); }
    public Task SetEmailConfirmedAsync(ApplicationUser user, bool value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); user.EmailConfirmed = value; return Task.CompletedTask; }
    public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.SecurityStamp); }
    public Task SetSecurityStampAsync(ApplicationUser user, string value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); user.SecurityStamp = value; return Task.CompletedTask; }
    public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.LockoutEnabled); }
    public Task SetLockoutEnabledAsync(ApplicationUser user, bool value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); user.LockoutEnabled = value; return Task.CompletedTask; }
    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.Id); }
    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.PasswordHash != null); }
    public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.LockoutEnd); }
    public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); user.LockoutEnd = value; return Task.CompletedTask; }
    public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(++user.AccessFailedCount); }
    public Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); user.AccessFailedCount = 0; return Task.CompletedTask; }
    public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(user.AccessFailedCount); }
}
