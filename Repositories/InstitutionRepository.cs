using Dapper;
using ProofTrail.Data;
using ProofTrail.Models;
using ProofTrail.ViewModels;
namespace ProofTrail.Repositories;
public class InstitutionRepository(SqlDatabase database) : IInstitutionRepository
{
    public async Task<Institution?> GetByUserAsync(string userId) =>
        await (await database.OpenAsync()).QuerySingleOrDefaultAsync<Institution>(
            "SELECT * FROM dbo.Institutions WHERE UserId = @userId", new { userId }, database.Transaction);
    public async Task<bool> RegistrationExistsAsync(string registrationNumber) =>
        await (await database.OpenAsync()).ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Institutions WHERE RegistrationNumber = @registrationNumber",
            new { registrationNumber }, database.Transaction) > 0;
    public async Task AddAsync(Institution institution)
    {
        institution.Id = await (await database.OpenAsync()).QuerySingleAsync<int>("""
            INSERT INTO dbo.Institutions (Name, RegistrationNumber, UserId, Status, AppliedAt)
            OUTPUT INSERTED.Id VALUES (@Name, @RegistrationNumber, @UserId, @Status, @AppliedAt)
            """, institution, database.Transaction);
    }
    public async Task<AdminViewModel> GetAdminDashboardAsync()
    {
        var connection = await database.OpenAsync();
        var institutions = await connection.QueryAsync<Institution, ApplicationUser, Institution>("""
            SELECT i.*, u.* FROM dbo.Institutions i
            JOIN dbo.AspNetUsers u ON u.Id = i.UserId ORDER BY i.AppliedAt DESC
            """, (institution, user) => { institution.User = user; return institution; }, splitOn: "Id");
        return new AdminViewModel {
            Institutions = institutions.ToList(),
            UserCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.AspNetUsers"),
            HolderCount = await connection.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId
                WHERE r.NormalizedName = 'HOLDER'
                """),
            NewTodayCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM dbo.AspNetUsers WHERE CreatedAt >= @today", new { today = DateTime.UtcNow.Date })
        };
    }
    public async Task<List<ApplicationUser>> GetUsersAsync() =>
        (await (await database.OpenAsync()).QueryAsync<ApplicationUser>(
            "SELECT Id, FullName, Email, CreatedAt FROM dbo.AspNetUsers ORDER BY FullName")).ToList();
}
