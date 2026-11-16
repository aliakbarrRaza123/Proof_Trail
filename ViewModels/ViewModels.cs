using ProofTrail.Models;
namespace ProofTrail.ViewModels;
public class AdminViewModel
{
    public List<Institution> Institutions { get; set; } = [];
    public int UserCount { get; set; }
    public int HolderCount { get; set; }
    public int NewTodayCount { get; set; }
}
