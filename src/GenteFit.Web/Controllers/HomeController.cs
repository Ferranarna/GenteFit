using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GenteFit.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index() => View(User.FindFirstValue(ClaimTypes.Role)!);
}