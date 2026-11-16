using ProofTrail.Models;
using ProofTrail.ViewModels;
namespace ProofTrail.Repositories;
public interface IInstitutionRepository
{
    Task<Institution?> GetByUserAsync(string userId);
    Task<bool> RegistrationExistsAsync(string registrationNumber);
    Task AddAsync(Institution institution);
    Task<AdminViewModel> GetAdminDashboardAsync();
    Task<List<ApplicationUser>> GetUsersAsync();
}
