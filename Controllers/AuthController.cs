using Microsoft.AspNetCore.Mvc;

namespace LastCallMotorAuctions.API.Controllers
{
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult Register() => View();
    }
}
