using ProofTrail.ViewModels;

namespace ProofTrail.Repositories;

public interface ICredentialRepository
{
    Task<bool> IssueAsync(Guid id, string issuerUserId, string normalizedHolderEmail, string title);
    Task<List<CredentialListItem>> GetForIssuerAsync(string issuerUserId);
    Task<List<CredentialListItem>> GetForHolderAsync(string holderUserId);
    Task<CredentialLookupResult?> FindPublicAsync(Guid id);
}
