using ProofTrail.Models;

namespace ProofTrail.Repositories;

public interface IInstitutionRepository
{
    Task<Institution?> GetByUserAsync(string userId);
    Task<bool> RegistrationExistsAsync(string registrationNumber);
    Task AddAsync(Institution institution);
    Task<List<Institution>> GetAllAsync();
    Task<bool> UpdateWebsiteAsync(string userId, string website);
    Task<bool> DecideAsync(int id, ApprovalStatus status);
}
