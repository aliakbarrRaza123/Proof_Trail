using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ProofTrail.Controllers;
// Public page: the Verifier needs no account.
[AllowAnonymous]
public class VerifyController : Controller
{
    [HttpGet]
    public IActionResult Index(string? id)
    {
        id = id?.Trim();
        ViewData["CredentialId"] = string.IsNullOrEmpty(id) ? null : (id.Length > 50 ? id[..50] : id);
        return View();
    }
}
