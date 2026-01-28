using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Controllers
{
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SellerController> _logger;

        public SellerController(ApplicationDbContext db, ILogger<SellerController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // TEMP: load all auctions (no seller filtering yet)
                var auctions = await _db.Auctions
                    .AsNoTracking()
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                return View(auctions);
            }
            catch (Exception ex)
            {
                // Log the real issue but don't crash the app
                _logger.LogError(ex, "Failed to load Seller Dashboard");

                // Show dashboard with empty data instead of 500 error
                ViewBag.DbError = "Database is not available yet.";
                return View(new List<Auction>());
            }
        }
    }
}
