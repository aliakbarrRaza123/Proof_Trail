using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using ProofTrail.Data;
namespace ProofTrail.IdentityStores;
public sealed class DapperRoleStore(SqlDatabase database) : IRoleStore<IdentityRole>
{
    public void Dispose() { }
    private CommandDefinition Command(string sql, object? args, CancellationToken ct) =>
        new(sql, args, database.Transaction, cancellationToken: ct);
    public async Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken ct)
    {
        try {
            await (await database.OpenAsync(ct)).ExecuteAsync(Command("""
                INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
                VALUES (@Id, @Name, @NormalizedName, @ConcurrencyStamp)
                """, role, ct));
            return IdentityResult.Success;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) {
            return IdentityResult.Failed(new IdentityErrorDescriber().DuplicateRoleName(role.Name ?? ""));
        }
    }
    public async Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken ct)
    {
        var newStamp = Guid.NewGuid().ToString();
        try {
            var count = await (await database.OpenAsync(ct)).ExecuteAsync(Command("""
                UPDATE dbo.AspNetRoles SET Name=@Name, NormalizedName=@NormalizedName, ConcurrencyStamp=@newStamp
                WHERE Id=@Id AND (ConcurrencyStamp=@ConcurrencyStamp OR (ConcurrencyStamp IS NULL AND @ConcurrencyStamp IS NULL))
                """, new { role.Id, role.Name, role.NormalizedName, role.ConcurrencyStamp, newStamp }, ct));
            if (count != 1) return IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure());
            role.ConcurrencyStamp = newStamp;
            return IdentityResult.Success;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) {
            return IdentityResult.Failed(new IdentityErrorDescriber().DuplicateRoleName(role.Name ?? ""));
        }
    }
    public async Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken ct)
    {
        // Roles assigned to users cannot be deleted: the FK protects them.
        var count = await (await database.OpenAsync(ct)).ExecuteAsync(Command("""
            DELETE FROM dbo.AspNetRoles WHERE Id=@Id
            AND (ConcurrencyStamp=@ConcurrencyStamp OR (ConcurrencyStamp IS NULL AND @ConcurrencyStamp IS NULL))
            """, role, ct));
        return count == 1 ? IdentityResult.Success : IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure());
    }
    public async Task<IdentityRole?> FindByIdAsync(string roleId, CancellationToken ct) =>
        await (await database.OpenAsync(ct)).QuerySingleOrDefaultAsync<IdentityRole>(Command(
            "SELECT * FROM dbo.AspNetRoles WHERE Id=@roleId", new { roleId }, ct));
    public async Task<IdentityRole?> FindByNameAsync(string normalizedRoleName, CancellationToken ct) =>
        await (await database.OpenAsync(ct)).QuerySingleOrDefaultAsync<IdentityRole>(Command(
            "SELECT * FROM dbo.AspNetRoles WHERE NormalizedName=@normalizedRoleName", new { normalizedRoleName }, ct));
    public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(role.Id); }
    public Task<string?> GetRoleNameAsync(IdentityRole role, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(role.Name); }
    public Task SetRoleNameAsync(IdentityRole role, string? value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); role.Name=value; return Task.CompletedTask; }
    public Task<string?> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(role.NormalizedName); }
    public Task SetNormalizedRoleNameAsync(IdentityRole role, string? value, CancellationToken ct) { ct.ThrowIfCancellationRequested(); role.NormalizedName=value; return Task.CompletedTask; }
}
