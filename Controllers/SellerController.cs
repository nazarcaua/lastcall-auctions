using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.Models;
using LastCallMotorAuctions.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<SellerController> _logger;

        public SellerController(
            ApplicationDbContext db,
            UserManager<User> userManager,
            ILogger<SellerController> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Get the logged-in seller
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var sellerId = user.Id;
                var now = DateTime.UtcNow;

                // Get all auctions for this seller's listings
                var auctions = await _db.Auctions
                    .AsNoTracking()
                    .Include(a => a.Listing)
                        .ThenInclude(l => l!.Make)
                    .Include(a => a.Listing)
                        .ThenInclude(l => l!.Model)
                    .Include(a => a.Status)
                    .Where(a => a.Listing != null && a.Listing.SellerId == sellerId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                var auctionIds = auctions.Select(a => a.AuctionId).ToList();

                // Count total bids across seller's auctions
                var totalBids = auctionIds.Count > 0
                    ? await _db.Bids.AsNoTracking().CountAsync(b => auctionIds.Contains(b.AuctionId))
                    : 0;

                // Calculate revenue from completed payments (StatusId = 2 = Captured)
                var totalRevenue = auctionIds.Count > 0
                    ? await _db.Payments.AsNoTracking()
                        .Where(p => auctionIds.Contains(p.AuctionId) && p.StatusId == 2)
                        .SumAsync(p => (decimal?)p.Amount) ?? 0m
                    : 0m;

                // Count "needs attention" items
                var endedAuctionIds = auctions
                    .Where(a => a.EndTime <= now)
                    .Select(a => a.AuctionId)
                    .ToList();

                var needsAttention = 0;
                if (endedAuctionIds.Count > 0)
                {
                    var auctionResultIds = await _db.AuctionResults
                        .AsNoTracking()
                        .Where(ar => endedAuctionIds.Contains(ar.AuctionId))
                        .Select(ar => ar.AuctionId)
                        .ToListAsync();

                    needsAttention = endedAuctionIds.Except(auctionResultIds).Count();

                    foreach (var auction in auctions.Where(a => a.EndTime <= now && a.ReservePrice.HasValue))
                    {
                        var highestBid = await _db.Bids
                            .AsNoTracking()
                            .Where(b => b.AuctionId == auction.AuctionId)
                            .MaxAsync(b => (decimal?)b.Amount) ?? 0m;

                        if (highestBid < auction.ReservePrice)
                            needsAttention++;
                    }
                }

                var viewModel = new SellerDashboardViewModel
                {
                    Auctions = auctions,
                    TotalListings = auctions.Count,
                    ActiveListings = auctions.Count(a => a.EndTime > now),
                    EndedListings = auctions.Count(a => a.EndTime <= now),
                    TotalBids = totalBids,
                    TotalRevenue = totalRevenue,
                    NeedsAttentionCount = needsAttention,
                    SellerId = sellerId,
                    SellerName = user.FullName ?? user.UserName ?? "Seller"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Seller Dashboard");

                ViewBag.DbError = "Database is not available yet.";
                return View(new SellerDashboardViewModel());
            }
        }
    }
}
