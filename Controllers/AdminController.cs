using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProofTrail.Models;
using ProofTrail.Repositories;
namespace ProofTrail.Controllers;
[Authorize(Roles = Roles.Admin)]
public class AdminController(IInstitutionRepository institutions) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await institutions.GetAdminDashboardAsync());
    [HttpGet]
    public async Task<IActionResult> Users() => View(await institutions.GetUsersAsync());
}
