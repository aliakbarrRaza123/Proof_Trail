using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProofTrail.Models;
namespace ProofTrail.Areas.Identity.Pages.Account;
[Authorize]
public class LogoutModel(SignInManager<ApplicationUser> signIn) : PageModel
{
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync() { await signIn.SignOutAsync(); return RedirectToAction("Index", "Home", new { area = "" }); }
}
