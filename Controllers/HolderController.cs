using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProofTrail.Models;
using ProofTrail.Repositories;
using ProofTrail.ViewModels;

namespace ProofTrail.Controllers;

[Authorize(Roles = Roles.Holder)]
public class HolderController(UserManager<ApplicationUser> users, ICredentialRepository credentials) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();

        return View(new HolderDashboardViewModel(user, await credentials.GetForHolderAsync(user.Id)));
    }
}
