using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProofTrail.Models;
namespace ProofTrail.Areas.Identity.Pages.Account;
[AllowAnonymous]
public class LoginModel(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string? ReturnUrl { get; set; }
    public bool AdminSetupRequired { get; set; }
    public class InputModel {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
        [Display(Name="Remember me")] public bool RememberMe { get; set; }
    }
    public async Task<IActionResult> OnGetAsync(string? returnUrl = null) {
        ReturnUrl = returnUrl;
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Dashboard", "Home", new { area = "" });
        AdminSetupRequired = (await users.GetUsersInRoleAsync(Roles.Admin)).Count == 0;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null) {
        ReturnUrl = returnUrl;
        AdminSetupRequired = (await users.GetUsersInRoleAsync(Roles.Admin)).Count == 0;
        if (!ModelState.IsValid) return Page();
        var user = await users.FindByEmailAsync(Input.Email.Trim());
        var result = user is null ? Microsoft.AspNetCore.Identity.SignInResult.Failed : await signIn.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded) {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction("Dashboard", "Home", new { area = "" });
        }
        ModelState.AddModelError("", result.IsLockedOut ? "Too many attempts. Try again after five minutes." : "Invalid email or password.");
        return Page();
    }
}
