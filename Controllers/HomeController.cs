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
    }
}
