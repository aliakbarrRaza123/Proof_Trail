using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProofTrail.Repositories;
using ProofTrail.ViewModels;

namespace ProofTrail.Controllers;

[AllowAnonymous]
public class VerifyController(ICredentialRepository credentials) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? id) => View(await FindAsync(id));

    [HttpGet]
    public async Task<IActionResult> Lookup(string? id) =>
        PartialView("_LookupResult", await FindAsync(id));

    private async Task<VerifyCredentialViewModel> FindAsync(string? id)
    {
        id = id?.Trim();
        if (string.IsNullOrEmpty(id)) return new(null, null, null);
        if (!Guid.TryParseExact(id, "D", out var credentialId))
            return new(null, null, "Enter the full credential ID shown on the Holder or Issuer dashboard.");

        var credential = await credentials.FindPublicAsync(credentialId);
        return new(id, credential, credential == null ? "No credential found for this ID." : null);
    }
}
