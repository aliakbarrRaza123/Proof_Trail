using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProofTrail.Models;
using ProofTrail.Repositories;
using ProofTrail.ViewModels;

namespace ProofTrail.Controllers;

[Authorize(Roles = Roles.Issuer)]
public class IssuerController(
    IInstitutionRepository institutions,
    ICredentialRepository credentials,
    UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var institution = await institutions.GetByUserAsync(userId);
        if (institution == null) return NotFound();

        return View(new IssuerDashboardViewModel(institution, await credentials.GetForIssuerAsync(userId), new()));
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var institution = await institutions.GetByUserAsync(userId);
        if (institution == null) return NotFound();

        return View(new EditInstitutionInput { OfficialWebsite = institution.OfficialWebsite });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditInstitutionInput input)
    {
        if (!ModelState.IsValid) return View(input);

        var website = input.OfficialWebsite!.Trim();
        if (!Uri.TryCreate(website, UriKind.Absolute, out var url)
            || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(url.Host))
        {
            ModelState.AddModelError(nameof(input.OfficialWebsite), "Enter an official website starting with https:// or http://.");
            return View(input);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await institutions.UpdateWebsiteAsync(userId, website)) return NotFound();

        TempData["Success"] = "Institution profile updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Issue([Bind(Prefix = "Issue")] IssueCredentialInput input)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var institution = await institutions.GetByUserAsync(userId);
        if (institution == null) return NotFound();
        if (institution.Status != ApprovalStatus.Approved) return Forbid();

        if (ModelState.IsValid)
        {
            var id = Guid.NewGuid();
            var holderEmail = users.NormalizeEmail(input.HolderEmail.Trim())!;
            if (await credentials.IssueAsync(id, userId, holderEmail, input.Title.Trim()))
            {
                TempData["Success"] = "Credential issued.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("Issue.HolderEmail", "Enter the email of a registered Holder.");
        }

        return View(nameof(Index), new IssuerDashboardViewModel(
            institution, await credentials.GetForIssuerAsync(userId), input));
    }
}
