using Microsoft.AspNetCore.Mvc;

namespace LastCallMotorAuctions.API.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
