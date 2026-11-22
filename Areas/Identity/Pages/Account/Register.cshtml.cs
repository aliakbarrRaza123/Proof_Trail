using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using ProofTrail.Repositories;
using ProofTrail.Data;
using ProofTrail.Models;
namespace ProofTrail.Areas.Identity.Pages.Account;
[AllowAnonymous]
public class RegisterModel(UserManager<ApplicationUser> users, SqlDatabase database, IInstitutionRepository institutions) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public class InputModel {
        [Required, RegularExpression("^(Holder|Issuer)$")] public string Role { get; set; } = Roles.Holder;
        [Required, StringLength(120), Display(Name="Full name / Institution name")] public string FullName { get; set; } = "";
        [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
        [StringLength(40), Display(Name = "Student roll number")] public string? RollNumber { get; set; }
        [StringLength(256), Display(Name = "Official website")] public string? OfficialWebsite { get; set; }
        [StringLength(80), Display(Name="Registration / HEC number")] public string? RegistrationNumber { get; set; }
        [Required, StringLength(100, MinimumLength=8), DataType(DataType.Password)] public string Password { get; set; } = "";
        [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name="Confirm password")] public string ConfirmPassword { get; set; } = "";
    }
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Dashboard", "Home", new { area = "" }) : Page();
    public async Task<IActionResult> OnPostAsync() {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Dashboard", "Home", new { area = "" });
        Input.RegistrationNumber = Input.RegistrationNumber?.Trim().ToUpperInvariant();
        Input.RollNumber = Input.RollNumber?.Trim();
        Input.OfficialWebsite = Input.OfficialWebsite?.Trim();
        if (Input.Role == Roles.Holder && string.IsNullOrWhiteSpace(Input.RollNumber))
            ModelState.AddModelError("Input.RollNumber", "Student roll number is required.");
        if (Input.Role == Roles.Issuer) {
            if (!Uri.TryCreate(Input.OfficialWebsite, UriKind.Absolute, out var website)
                || (website.Scheme != Uri.UriSchemeHttp && website.Scheme != Uri.UriSchemeHttps)
                || string.IsNullOrWhiteSpace(website.Host))
                ModelState.AddModelError("Input.OfficialWebsite", "Enter an official website starting with https:// or http://.");
            if (string.IsNullOrWhiteSpace(Input.RegistrationNumber)) ModelState.AddModelError("Input.RegistrationNumber", "Registration number is required for an institution.");
            else if (await institutions.RegistrationExistsAsync(Input.RegistrationNumber)) ModelState.AddModelError("Input.RegistrationNumber", "This institution registration number is already registered.");
        }
        if (!ModelState.IsValid) return Page();
        await database.BeginAsync();
        try {
        var email = Input.Email.Trim();
        var user = new ApplicationUser {
            UserName = email, Email = email, FullName = Input.FullName.Trim(),
            RollNumber = Input.Role == Roles.Holder ? Input.RollNumber : null
        };
        var result = await users.CreateAsync(user, Input.Password);
        if (!result.Succeeded) { foreach (var e in result.Errors) ModelState.AddModelError("", e.Description); return Page(); }
        var roleResult = await users.AddToRoleAsync(user, Input.Role);
        if (!roleResult.Succeeded) { foreach (var e in roleResult.Errors) ModelState.AddModelError("", e.Description); return Page(); }
        if (Input.Role == Roles.Issuer) await institutions.AddAsync(new() { Name = user.FullName, RegistrationNumber = Input.RegistrationNumber!, OfficialWebsite = Input.OfficialWebsite, UserId = user.Id });
        await database.CommitAsync();
        TempData["Success"] = Input.Role == Roles.Issuer ? "Institution account created. Log in to view your saved institution details." : "Account created. You can now log in.";
        return RedirectToPage("Login");
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627) {
            ModelState.AddModelError("", "Email or institution registration number already exists.");
            return Page();
        }
        finally { await database.RollbackAsync(); }
    }
}
