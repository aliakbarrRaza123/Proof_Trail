using Dapper;
using ProofTrail.Data;
using ProofTrail.Models;
using ProofTrail.ViewModels;

namespace ProofTrail.Repositories;

public class CredentialRepository(SqlDatabase database) : ICredentialRepository
{
    public async Task<bool> IssueAsync(Guid id, string issuerUserId, string normalizedHolderEmail, string title) =>
        await (await database.OpenAsync()).ExecuteAsync("""
            INSERT INTO dbo.IssuedCredentials (Id, InstitutionId, HolderUserId, Title)
            SELECT @id, i.Id, u.Id, @title
            FROM dbo.Institutions i
            CROSS JOIN dbo.AspNetUsers u
            WHERE i.UserId = @issuerUserId AND i.Status = @approved
              AND u.NormalizedEmail = @normalizedHolderEmail
              AND EXISTS (
                  SELECT 1 FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id = ur.RoleId
                  WHERE ur.UserId = u.Id AND r.NormalizedName = 'HOLDER'
              )
            """, new { id, issuerUserId, normalizedHolderEmail, title, approved = ApprovalStatus.Approved }) == 1;

    public async Task<List<CredentialListItem>> GetForIssuerAsync(string issuerUserId) =>
        (await (await database.OpenAsync()).QueryAsync<CredentialListItem>("""
            SELECT c.Id, c.Title, c.IssuedAt, u.FullName AS HolderName
            FROM dbo.IssuedCredentials c
            JOIN dbo.Institutions i ON i.Id = c.InstitutionId
            JOIN dbo.AspNetUsers u ON u.Id = c.HolderUserId
            WHERE i.UserId = @issuerUserId
            ORDER BY c.IssuedAt DESC, c.Id
            """, new { issuerUserId })).ToList();

    public async Task<List<CredentialListItem>> GetForHolderAsync(string holderUserId) =>
        (await (await database.OpenAsync()).QueryAsync<CredentialListItem>("""
            SELECT c.Id, c.Title, c.IssuedAt, i.Name AS InstitutionName
            FROM dbo.IssuedCredentials c
            JOIN dbo.Institutions i ON i.Id = c.InstitutionId
            WHERE c.HolderUserId = @holderUserId
            ORDER BY c.IssuedAt DESC, c.Id
            """, new { holderUserId })).ToList();

    public async Task<CredentialLookupResult?> FindPublicAsync(Guid id) =>
        await (await database.OpenAsync()).QuerySingleOrDefaultAsync<CredentialLookupResult>("""
            SELECT c.Id, c.Title, c.IssuedAt, i.Name AS InstitutionName
            FROM dbo.IssuedCredentials c
            JOIN dbo.Institutions i ON i.Id = c.InstitutionId
            WHERE c.Id = @id
            """, new { id });
}
