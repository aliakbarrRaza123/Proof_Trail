using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProofTrail.Models;
namespace ProofTrail.Controllers;
[Authorize(Roles = Roles.Holder)]
public class HolderController(UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Challenge();
        return View(user);
    }
}
