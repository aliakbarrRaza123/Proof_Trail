using System.ComponentModel.DataAnnotations;
using ProofTrail.Models;
namespace ProofTrail.ViewModels;
public class AdminViewModel
{
    public List<Institution> Institutions { get; set; } = [];
    public int UserCount { get; set; }
    public int HolderCount { get; set; }
    public int NewTodayCount { get; set; }
}

public class AdminUserRow
{
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? RoleNames { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserCounts
{
    public int Total { get; set; }
    public int Holders { get; set; }
    public int NewToday { get; set; }
}

public class EditInstitutionInput
{
    [Required]
    [StringLength(256)]
    [Display(Name = "Official website")]
    public string? OfficialWebsite { get; set; }
}

public class IssueCredentialInput
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Holder email")]
    public string HolderEmail { get; set; } = "";

    [Required]
    [StringLength(160)]
    [Display(Name = "Credential title")]
    public string Title { get; set; } = "";
}

public record IssuerDashboardViewModel(Institution Institution, List<CredentialListItem> Credentials, IssueCredentialInput Issue);
public record HolderDashboardViewModel(ApplicationUser Account, List<CredentialListItem> Credentials);

public class CredentialListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public DateTime IssuedAt { get; set; }
    public string? HolderName { get; set; }
    public string? InstitutionName { get; set; }
}

public class CredentialLookupResult
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string InstitutionName { get; set; } = "";
    public DateTime IssuedAt { get; set; }
}

public record VerifyCredentialViewModel(string? Id, CredentialLookupResult? Credential, string? Message);
