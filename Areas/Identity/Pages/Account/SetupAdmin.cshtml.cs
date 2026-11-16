using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using ProofTrail.Data;
using ProofTrail.Models;
namespace ProofTrail.Areas.Identity.Pages.Account;
// First-run setup: creates the Admin account once. Disabled as soon as an Admin exists,
// and only opens on the computer that runs the application (localhost).
[AllowAnonymous]
public class SetupAdminModel(UserManager<ApplicationUser> users, SqlDatabase database) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public class InputModel {
        [Required, StringLength(120), Display(Name="Full name")] public string FullName { get; set; } = "";
        [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
        [Required, StringLength(100, MinimumLength=8), DataType(DataType.Password)] public string Password { get; set; } = "";
        [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name="Confirm password")] public string ConfirmPassword { get; set; } = "";
    }
    private bool IsLocalRequest() {
        var ip = HttpContext.Connection.RemoteIpAddress;
        return ip != null && IPAddress.IsLoopback(ip);
    }
    private async Task<IActionResult?> BlockedAsync() {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Dashboard", "Home", new { area = "" });
        if (!IsLocalRequest()) {
            TempData["Error"] = "Admin setup can only be opened on the computer that runs the application (localhost).";
            return RedirectToPage("Login");
        }
        if ((await users.GetUsersInRoleAsync(Roles.Admin)).Count > 0) {
            TempData["Error"] = "An Admin account already exists. Please log in.";
            return RedirectToPage("Login");
        }
        return null;
    }
    public async Task<IActionResult> OnGetAsync() => await BlockedAsync() ?? Page();
    public async Task<IActionResult> OnPostAsync() {
        var blocked = await BlockedAsync();
        if (blocked != null) return blocked;
        if (!ModelState.IsValid) return Page();
        await database.BeginAsync();
        try {
            var email = Input.Email.Trim();
            var user = new ApplicationUser { UserName = email, Email = email, FullName = Input.FullName.Trim() };
            var result = await users.CreateAsync(user, Input.Password);
            if (!result.Succeeded) { foreach (var e in result.Errors) ModelState.AddModelError("", e.Description); return Page(); }
            var roleResult = await users.AddToRoleAsync(user, Roles.Admin);
            if (!roleResult.Succeeded) { foreach (var e in roleResult.Errors) ModelState.AddModelError("", e.Description); return Page(); }
            await database.CommitAsync();
            TempData["Success"] = "Admin account created. Log in to continue.";
            return RedirectToPage("Login");
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) {
            ModelState.AddModelError("", "An account with this email already exists.");
            return Page();
        }
        finally { await database.RollbackAsync(); }
    }
}
