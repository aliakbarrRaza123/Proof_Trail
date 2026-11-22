using Dapper;
using ProofTrail.Data;
using ProofTrail.ViewModels;

namespace ProofTrail.Repositories;

public class UserRepository(SqlDatabase database) : IUserRepository
{
    public async Task<List<AdminUserRow>> SearchAsync(string? search)
    {
        search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var connection = await database.OpenAsync();
        var users = await connection.QueryAsync<AdminUserRow>("""
            SELECT u.FullName, u.Email, u.CreatedAt, roles.RoleNames
            FROM dbo.AspNetUsers u
            OUTER APPLY (
                SELECT STRING_AGG(r.Name, ', ') WITHIN GROUP (ORDER BY r.Name) AS RoleNames
                FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId
                WHERE ur.UserId = u.Id
            ) roles
            WHERE @search IS NULL OR CHARINDEX(@search, u.FullName) > 0 OR CHARINDEX(@search, u.Email) > 0
            ORDER BY u.FullName, u.Email
            """, new { search });
        return users.ToList();
    }

    public async Task<UserCounts> GetCountsAsync() =>
        await (await database.OpenAsync()).QuerySingleAsync<UserCounts>("""
            SELECT COUNT(*) AS Total,
                (SELECT COUNT(*) FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId
                 WHERE r.NormalizedName = 'HOLDER') AS Holders,
                COUNT(CASE WHEN CreatedAt >= @today THEN 1 END) AS NewToday
            FROM dbo.AspNetUsers
            """, new { today = DateTime.UtcNow.Date });
}
