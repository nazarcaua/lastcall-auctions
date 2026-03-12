using Microsoft.AspNetCore.Mvc;

namespace LastCallMotorAuctions.API.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Policies()
        {
            return View();
        }

        [HttpGet]
        public IActionResult VehicleValue()
        {
            return View();
        }
    }
}
