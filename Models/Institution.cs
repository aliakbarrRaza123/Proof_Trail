using System.ComponentModel.DataAnnotations;
namespace ProofTrail.Models;
public enum ApprovalStatus { Pending, Approved, Rejected }
public class Institution
{
    public int Id { get; set; }
    [MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(80)] public string RegistrationNumber { get; set; } = "";
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}
