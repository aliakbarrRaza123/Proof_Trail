using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace ProofTrail.Models;
public class ApplicationUser : IdentityUser
{
    [MaxLength(120)] public string FullName { get; set; } = "";
    [MaxLength(40)] public string? RollNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public static class Roles
{
    public const string Admin = "Admin", Issuer = "Issuer", Holder = "Holder";
}
