using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Models;

namespace LastCallMotorAuctions.API.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<User> _signInManager;

        public AuthController(SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult Register() => View();

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
