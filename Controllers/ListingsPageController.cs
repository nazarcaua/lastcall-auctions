using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LastCallMotorAuctions.API.Controllers
{
    [Authorize(Roles = "Seller")]
    public class ListingsPageController : Controller
    {
        [HttpGet("/Listings/Create")]
        public IActionResult Create()
        {
            // Serve the Razor view for creating listings
            return View("~/Views/Listings/Create.cshtml");
        }
    }
}
