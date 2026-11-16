using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace ProofTrail.Controllers;
public class HomeController : Controller
{
    public IActionResult Index() => View();
    [Authorize] public IActionResult Dashboard() => RedirectToAction("Index", User.IsInRole("Admin") ? "Admin" : User.IsInRole("Issuer") ? "Issuer" : "Holder");
    [ResponseCache(Duration=0, Location=ResponseCacheLocation.None, NoStore=true)]
    public IActionResult Error() { Response.StatusCode = 500; return View(); }
}
