using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.Models;
using LastCallMotorAuctions.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace LastCallMotorAuctions.API.Controllers
{
    [Authorize(Roles = "Buyer")]
    [Route("Buyer")]
    public class BuyerPageController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public BuyerPageController(ApplicationDbContext db, UserManager<User> userManager, IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        private string? GetFirstPhotoUrl(int listingId)
        {
            var uploadDir = Path.Combine(_env.WebRootPath ?? "", "uploads", "listings", listingId.ToString());
            if (!Directory.Exists(uploadDir)) return null;

            var file = Directory.GetFiles(uploadDir)
                .Where(f => AllowedPhotoExtensions.Contains(Path.GetExtension(f)))
                .OrderBy(f => f)
                .FirstOrDefault();

            return file == null ? null : $"/uploads/listings/{listingId}/{Path.GetFileName(file)}";
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Auth");

            var buyerId = user.Id;
            var now = DateTime.UtcNow;

            var bids = await _db.Bids
                .AsNoTracking()
                .Where(b => b.BidderId == buyerId)
                .Include(b => b.Auction)
                    .ThenInclude(a => a!.Listing)
                        .ThenInclude(l => l!.Make)
                .Include(b => b.Auction)
                    .ThenInclude(a => a!.Listing)
                        .ThenInclude(l => l!.Model)
                .Include(b => b.Auction)
                    .ThenInclude(a => a!.Status)
                .OrderByDescending(b => b.PlacedAt)
                .ToListAsync();

            var auctionIds = bids.Select(b => b.AuctionId).Distinct().ToList();

            var currentHighestBids = auctionIds.Count > 0
                ? await _db.Bids
                    .AsNoTracking()
                    .Where(b => auctionIds.Contains(b.AuctionId))
                    .GroupBy(b => b.AuctionId)
                    .Select(g => new { AuctionId = g.Key, MaxBid = g.Max(b => b.Amount) })
                    .ToDictionaryAsync(x => x.AuctionId, x => x.MaxBid)
                : new Dictionary<int, decimal>();

            var myHighestBidPerAuction = bids
                .GroupBy(b => b.AuctionId)
                .ToDictionary(g => g.Key, g => g.Max(b => b.Amount));

            var watchlistItems = await _db.Favourites
                .AsNoTracking()
                .Where(f => f.UserId == buyerId)
                .Include(f => f.Auction)
                    .ThenInclude(a => a!.Listing)
                        .ThenInclude(l => l!.Make)
                .Include(f => f.Auction)
                    .ThenInclude(a => a!.Listing)
                        .ThenInclude(l => l!.Model)
                .Include(f => f.Auction)
                    .ThenInclude(a => a!.Status)
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync();

            var watchlistAuctionIds = watchlistItems.Select(f => f.AuctionId).ToList();
            var watchlistHighestBids = watchlistAuctionIds.Count > 0
                ? await _db.Bids
                    .AsNoTracking()
                    .Where(b => watchlistAuctionIds.Contains(b.AuctionId))
                    .GroupBy(b => b.AuctionId)
                    .Select(g => new { AuctionId = g.Key, MaxBid = g.Max(b => b.Amount) })
                    .ToDictionaryAsync(x => x.AuctionId, x => x.MaxBid)
                : new Dictionary<int, decimal>();

            var notifications = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == buyerId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var activeBidCount = auctionIds.Count(aid =>
            {
                var auction = bids.FirstOrDefault(b => b.AuctionId == aid)?.Auction;
                return auction != null && auction.EndTime > now;
            });

            var wonCount = auctionIds.Count(aid =>
            {
                var auction = bids.FirstOrDefault(b => b.AuctionId == aid)?.Auction;
                if (auction == null || auction.EndTime > now) return false;
                var myMax = myHighestBidPerAuction.GetValueOrDefault(aid);
                var currentMax = currentHighestBids.GetValueOrDefault(aid);
                return myMax > 0 && myMax == currentMax;
            });

            ViewBag.ActiveBidCount = activeBidCount;
            ViewBag.WatchlistCount = watchlistItems.Count;
            ViewBag.WonCount = wonCount;
            ViewBag.AlertCount = notifications.Count(n => !n.IsRead);
            ViewBag.CurrentHighestBids = currentHighestBids;
            ViewBag.MyHighestBids = myHighestBidPerAuction;
            ViewBag.WatchlistHighestBids = watchlistHighestBids;
            ViewBag.Now = now;

            var vm = new BuyerDashboardViewModel
            {
                BuyerId = buyerId,
                BuyerName = user.FullName,
                BidList = bids,
                Favourites = watchlistItems
            };

            return View(vm);
        }

        [HttpGet("WonAuctions")]
        public async Task<IActionResult> WonAuctions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var buyerId = user.Id;
            var now = DateTime.UtcNow;

            var bids = await _db.Bids
                .AsNoTracking()
                .Where(b => b.BidderId == buyerId)
                .Include(b => b.Auction)
                    .ThenInclude(a => a!.Listing)
                        .ThenInclude(l => l!.Make)
                .Include(b => b.Auction)
                    .ThenInclude(a => a!.Listing)
                        .ThenInclude(l => l!.Model)
                .ToListAsync();

            var endedAuctionIds = bids
                .Where(b => b.Auction != null && b.Auction.EndTime <= now)
                .Select(b => b.AuctionId)
                .Distinct()
                .ToList();

            // For each ended auction get the top bid amount and the winner's bidder ID
            Dictionary<int, (decimal MaxBid, int WinnerId)> highestBids;
            if (endedAuctionIds.Count > 0)
            {
                highestBids = await _db.Bids
                    .AsNoTracking()
                    .Where(b => endedAuctionIds.Contains(b.AuctionId))
                    .GroupBy(b => b.AuctionId)
                    .Select(g => new
                    {
                        AuctionId = g.Key,
                        MaxBid = g.Max(b => b.Amount),
                        WinnerId = g.OrderByDescending(b => b.Amount).Select(b => b.BidderId).First()
                    })
                    .ToDictionaryAsync(x => x.AuctionId, x => (x.MaxBid, x.WinnerId));
            }
            else
            {
                highestBids = new Dictionary<int, (decimal, int)>();
            }

            var myHighestBids = bids
                .Where(b => endedAuctionIds.Contains(b.AuctionId))
                .GroupBy(b => b.AuctionId)
                .ToDictionary(g => g.Key, g => g.Max(b => b.Amount));

            var wonAuctions = endedAuctionIds
                .Where(aid =>
                {
                    if (!highestBids.TryGetValue(aid, out var top)) return false;
                    var myMax = myHighestBids.GetValueOrDefault(aid);
                    return myMax > 0 && top.WinnerId == buyerId && myMax == top.MaxBid;
                })
                .Select(aid =>
                {
                    var bid = bids.First(b => b.AuctionId == aid);
                    var auction = bid.Auction!;
                    var listing = auction.Listing;
                    var title = listing != null
                        ? $"{listing.Year} {listing.Make?.Name ?? ""} {listing.Model?.Name ?? ""}".Trim()
                        : $"Auction #{aid}";
                    var winningAmount = myHighestBids[aid];
                    return new WonAuctionViewModel
                    {
                        AuctionId = aid,
                        VehicleTitle = title,
                        WinningBid = winningAmount,
                        EndTime = auction.EndTime,
                        FirstPhotoUrl = listing != null ? GetFirstPhotoUrl(listing.ListingId) : null
                    };
                })
                .OrderByDescending(w => w.EndTime)
                .ToList();

            ViewBag.WonCount = wonAuctions.Count;

            var vm = new BuyerDashboardViewModel
            {
                BuyerId = buyerId,
                BuyerName = user.FullName,
                WonAuctions = wonAuctions
            };

            return View(vm);
        }

        [HttpPost("Watchlist/Toggle/{auctionId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWatchlist(int auctionId, [FromForm] string? returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var existing = await _db.Favourites
                .FirstOrDefaultAsync(f => f.UserId == user.Id && f.AuctionId == auctionId);

            bool isFavourited;
            if (existing != null)
            {
                _db.Favourites.Remove(existing);
                await _db.SaveChangesAsync();
                isFavourited = false;
            }
            else
            {
                var auctionExists = await _db.Auctions.AnyAsync(a => a.AuctionId == auctionId);
                if (!auctionExists) return NotFound();
                _db.Favourites.Add(new Favourite { UserId = user.Id, AuctionId = auctionId });
                await _db.SaveChangesAsync();
                isFavourited = true;
            }

            // AJAX request — return JSON
            if (Request.Headers.Accept.ToString().Contains("application/json"))
                return Json(new { isFavourited });

            // Form POST — redirect back
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Dashboard");
        }

        [HttpGet("Watchlist")]
        public async Task<IActionResult> Watchlist()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var buyerId = user.Id;
            var now = DateTime.UtcNow;

            var watchlistItems = await _db.Favourites
                .AsNoTracking()
                .Where(f => f.UserId == buyerId)
                .Include(f => f.Auction)
                    .ThenInclude(a => a!.Listing)
                        .ThenInclude(l => l!.Make)
                .Include(f => f.Auction)
                    .ThenInclude(a => a!.Listing)
                        .ThenInclude(l => l!.Model)
                .Include(f => f.Auction)
                    .ThenInclude(a => a!.Status)
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync();

            var watchlistAuctionIds = watchlistItems.Select(f => f.AuctionId).ToList();
            var watchlistHighestBids = watchlistAuctionIds.Count > 0
                ? await _db.Bids
                    .AsNoTracking()
                    .Where(b => watchlistAuctionIds.Contains(b.AuctionId))
                    .GroupBy(b => b.AuctionId)
                    .Select(g => new { AuctionId = g.Key, MaxBid = g.Max(b => b.Amount) })
                    .ToDictionaryAsync(x => x.AuctionId, x => x.MaxBid)
                : new Dictionary<int, decimal>();

            ViewBag.WatchlistHighestBids = watchlistHighestBids;
            ViewBag.Now = now;

            return View(watchlistItems);
        }
    }
}
