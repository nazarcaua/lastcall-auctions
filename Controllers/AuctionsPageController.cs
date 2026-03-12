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
        [HttpPost("/Auctions/End/{id:int}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> End(int id)
        {
            // Get current user id
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                return Forbid();

            var now = DateTime.UtcNow;

            // First, try to find an auction group with this ID and end all its auctions
            try
            {
                var groupJoins = await _db.AuctionGroupAuctions
                    .Where(aga => aga.AuctionGroupId == id)
                    .ToListAsync();

                if (groupJoins.Count > 0)
                {
                    var auctionIds = groupJoins.Select(j => j.AuctionId).ToList();
                    var auctions = await _db.Auctions
                        .Include(a => a.Listing)
                        .Where(a => auctionIds.Contains(a.AuctionId))
                        .ToListAsync();

                    // Verify all auctions belong to this seller
                    if (auctions.Any(a => a.Listing == null || a.Listing.SellerId != userId))
                        return Forbid();

                    foreach (var auction in auctions.Where(a => a.EndTime > now || a.StatusId == 2))
                    {
                        auction.EndTime = now;
                        auction.StatusId = 3; // Ended
                    }

                    await _db.SaveChangesAsync();
                    return RedirectToAction("Dashboard", "Seller");
                }
            }
            catch
            {
                // AuctionGroup tables may not exist; fall through to single-auction lookup
            }

            // Fall back to ending a single auction by its AuctionId
            var singleAuction = await _db.Auctions
                .Include(a => a.Listing)
                .FirstOrDefaultAsync(a => a.AuctionId == id);

            if (singleAuction == null)
                return NotFound();

            if (singleAuction.Listing == null || singleAuction.Listing.SellerId != userId)
                return Forbid();

            singleAuction.EndTime = now;
            singleAuction.StatusId = 3; // Ended

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

            // First check if this is an auction group
            var groupAuctions = await _db.AuctionGroupAuctions
                .Where(aga => aga.AuctionGroupId == id)
                .Include(aga => aga.Auction)
                    .ThenInclude(a => a!.Listing)
                .Include(aga => aga.AuctionGroup)
                .ToListAsync();

            if (groupAuctions.Any())
            {
                // Verify seller owns all auctions in the group
                if (groupAuctions.Any(aga => aga.Auction?.Listing?.SellerId != userId))
                    return Forbid();

                ViewData["IsGroup"] = true;
                ViewData["GroupId"] = id;
                ViewData["GroupTitle"] = groupAuctions.First().AuctionGroup?.Title ?? "Auction Group";
                ViewData["AuctionIds"] = groupAuctions.Select(aga => aga.AuctionId).ToList();
                return View("~/Views/Auctions/Edit.cshtml");
            }

            // Fall back to single auction
            var auction = await _db.Auctions
                .Include(a => a.Listing)
                .FirstOrDefaultAsync(a => a.AuctionId == id);

            if (auction == null) return NotFound();
            if (auction.Listing == null || auction.Listing.SellerId != userId) return Forbid();

            ViewData["IsGroup"] = false;
            ViewData["AuctionId"] = id;
            return View("~/Views/Auctions/Edit.cshtml");
        }
    }
}
