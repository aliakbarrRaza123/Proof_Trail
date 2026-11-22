using ProofTrail.ViewModels;

namespace ProofTrail.Repositories;

public interface IUserRepository
{
    Task<List<AdminUserRow>> SearchAsync(string? search);
    Task<UserCounts> GetCountsAsync();
}
