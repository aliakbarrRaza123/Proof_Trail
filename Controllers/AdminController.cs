using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProofTrail.Models;
using ProofTrail.Repositories;
using ProofTrail.ViewModels;

namespace ProofTrail.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController(IInstitutionRepository institutions, IUserRepository users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var registeredInstitutions = await institutions.GetAllAsync();
        var counts = await users.GetCountsAsync();
        return View(new AdminViewModel {
            Institutions = registeredInstitutions,
            UserCount = counts.Total,
            HolderCount = counts.Holders,
            NewTodayCount = counts.NewToday
        });
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? search)
    {
        search = search?.Trim();
        ViewData["Search"] = search;
        return View(await users.SearchAsync(search));
    }

    [HttpGet]
    public async Task<IActionResult> SearchUsers(string? search) =>
        PartialView("_UsersTable", await users.SearchAsync(search));

    [HttpPost]
    public async Task<IActionResult> DecideInstitution(int id, ApprovalStatus status)
    {
        if (!ModelState.IsValid || id <= 0 || status is not (ApprovalStatus.Approved or ApprovalStatus.Rejected))
            return BadRequest("Choose a valid institution and decision.");

        if (!await institutions.DecideAsync(id, status))
            return Conflict("This application is no longer pending. Reload to see its current status.");

        return Json(new { status = status.ToString() });
    }
}
