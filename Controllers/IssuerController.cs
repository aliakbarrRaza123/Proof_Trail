using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProofTrail.Models;
using ProofTrail.Repositories;
namespace ProofTrail.Controllers;
[Authorize(Roles = Roles.Issuer)]
public class IssuerController(IInstitutionRepository institutions) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var institution = await institutions.GetByUserAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (institution == null) return NotFound();
        return View(institution);
    }
}
