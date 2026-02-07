using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using LastCallMotorAuctions.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LastCallMotorAuctions.API.Controllers
{
    // This controller serves the Razor view at /Auctions backed by the auction service
    public class AuctionsPageController : Controller
    {
        private readonly IAuctionService _auctionService;
        private readonly ApplicationDbContext _db;

        public AuctionsPageController(IAuctionService auctionService, ApplicationDbContext db)
        {
            _auctionService = auctionService;
            _db = db;
        }

        [HttpGet("/Auctions")]
        public async Task<IActionResult> Index()
        {
            var list = await _auctionService.GetActiveAuctionAsync();
            // The view file is located at Views/Auctions/Index.cshtml, not Views/AuctionsPage/Index.cshtml
            return View("~/Views/Auctions/Index.cshtml", list);
        }

        [HttpGet("/Auctions/End/{id:int}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> End(int id)
        {
            // Get current user id
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                return Forbid();

            var auction = await _db.Auctions
                .Include(a => a.Listing)
                .FirstOrDefaultAsync(a => a.AuctionId == id);

            if (auction == null)
                return NotFound();

            if (auction.Listing == null || auction.Listing.SellerId != userId)
                return Forbid();

            // End auction now
            auction.EndTime = DateTime.UtcNow;
            auction.StatusId = 3; // Ended

            await _db.SaveChangesAsync();

            return RedirectToAction("Dashboard", "Seller");
        }

        [HttpGet("/Auctions/Edit/{id:int}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> Edit(int id)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                return Forbid();

            var auction = await _db.Auctions
                .Include(a => a.Listing)
                .FirstOrDefaultAsync(a => a.AuctionId == id);

            if (auction == null) return NotFound();
            if (auction.Listing == null || auction.Listing.SellerId != userId) return Forbid();

            return View("~/Views/Auctions/Edit.cshtml");
        }
    }
}
