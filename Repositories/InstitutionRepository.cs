using Dapper;
using ProofTrail.Data;
using ProofTrail.Models;

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
            INSERT INTO dbo.Institutions (Name, RegistrationNumber, UserId, Status, AppliedAt, OfficialWebsite)
            OUTPUT INSERTED.Id VALUES (@Name, @RegistrationNumber, @UserId, @Status, @AppliedAt, @OfficialWebsite)
            """, institution, database.Transaction);
    }

    public async Task<List<Institution>> GetAllAsync()
    {
        var connection = await database.OpenAsync();
        var institutions = await connection.QueryAsync<Institution, ApplicationUser, Institution>("""
            SELECT i.*, u.Id, u.Email FROM dbo.Institutions i
            JOIN dbo.AspNetUsers u ON u.Id = i.UserId ORDER BY i.AppliedAt DESC
            """, (institution, user) => { institution.User = user; return institution; }, splitOn: "Id");
        return institutions.ToList();
    }

    public async Task<bool> UpdateWebsiteAsync(string userId, string website) =>
        await (await database.OpenAsync()).ExecuteAsync("""
            UPDATE dbo.Institutions SET OfficialWebsite = @website
            WHERE UserId = @userId
            """, new { userId, website }, database.Transaction) == 1;

    public async Task<bool> DecideAsync(int id, ApprovalStatus status) =>
        await (await database.OpenAsync()).ExecuteAsync("""
            UPDATE dbo.Institutions SET Status = @status
            WHERE Id = @id AND Status = @pending
            """, new { id, status, pending = ApprovalStatus.Pending }) == 1;
}
