using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Services;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;


        public AuctionsPageController(IAuctionService auctionService, ApplicationDbContext db, UserManager<User> userManager, INotificationService notificationService)
        {
            _auctionService = auctionService;
            _db = db;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [HttpGet("/Auctions")]
        public async Task<IActionResult> Index()
        {
            var list = await _auctionService.GetActiveAuctionAsync();
            await SetWatchlistViewBag();
            return View("~/Views/Auctions/Index.cshtml", list);
        }

        [HttpGet("/Auctions/End/{id:int}")]
        [HttpPost("/Auctions/End/{id:int}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> End(int id)
        {
            // Get current user id
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                return Forbid();

            var isAdmin = User.IsInRole("Admin");
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

                    // Verify all auctions belong to this seller (or user is admin)
                    if (!isAdmin && auctions.Any(a => a.Listing == null || a.Listing.SellerId != userId))
                        return Forbid();

                    if (auctions.Any(a => a.StartTime > now))
                    {
                        TempData["Error"] = "One or more auctions in this group haven’t started yet, so the group can’t be ended.";
                        return isAdmin
                            ? RedirectToAction("Dashboard", "Admin")
                            : RedirectToAction("Dashboard", "Seller");
                    }

                    foreach (var auction in auctions.Where(a => a.EndTime > now || a.StatusId == 2))
                    {
                        auction.EndTime = now;
                        auction.StatusId = 3; // Ended
                    }

                    await _db.SaveChangesAsync();
                    return isAdmin ? RedirectToAction("Dashboard", "Admin") : RedirectToAction("Dashboard", "Seller");
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

            // Allow if admin or if user owns the auction
            if (!isAdmin && (singleAuction.Listing == null || singleAuction.Listing.SellerId != userId))
                return Forbid();

            if (singleAuction.StartTime > now)
            {
                TempData["Error"] = "This auction hasn’t started yet, so it can’t be ended.";
                return isAdmin
                    ? RedirectToAction("Dashboard", "Admin")
                    : RedirectToAction("Dashboard", "Seller");
            }

            singleAuction.EndTime = now;
            singleAuction.StatusId = 3; // Ended

            await _db.SaveChangesAsync();

            if (singleAuction.Listing != null)
            {
                var singleSellerId = singleAuction.Listing.SellerId;
                await _notificationService.CreateAsync(singleSellerId, "AuctionEnded", "Auction ended",
                    "Your auction has ended.", auctionId: singleAuction.AuctionId);

                var singleWinnerId = await _db.Bids
                    .Where(b => b.AuctionId == singleAuction.AuctionId)
                    .OrderByDescending(b => b.Amount)
                    .Select(b => b.BidderId)
                    .FirstOrDefaultAsync();
                if (singleWinnerId != 0)
                    await _notificationService.CreateAsync(singleWinnerId, "AuctionWon", "You won the auction",
                        "You had the highest bid when the auction ended.", auctionId: singleAuction.AuctionId);
            }

            return isAdmin ? RedirectToAction("Dashboard", "Admin") : RedirectToAction("Dashboard", "Seller");
        }

        [HttpGet("/Auctions/Edit/{id:int}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                return Forbid();

            var isAdmin = User.IsInRole("Admin");
            var now = DateTime.UtcNow;

            // First check if this is an auction group
            var groupAuctions = await _db.AuctionGroupAuctions
                .Where(aga => aga.AuctionGroupId == id)
                .Include(aga => aga.Auction)
                    .ThenInclude(a => a!.Listing)
                .Include(aga => aga.AuctionGroup)
                .ToListAsync();

            if (groupAuctions.Any())
            {
                // Verify seller owns all auctions in the group (or user is admin)
                if (!isAdmin && groupAuctions.Any(aga => aga.Auction?.Listing?.SellerId != userId))
                    return Forbid();

                var allEnded = groupAuctions.All(aga => aga.Auction != null && aga.Auction.EndTime <= now);
                if (allEnded)
                {
                    TempData["Error"] = "This auction has ended and can’t be edited.";
                    return isAdmin
                        ? RedirectToAction("Dashboard", "Admin")
                        : RedirectToAction("Dashboard", "Seller");
                }

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

            // Allow if admin or if user owns the auction
            if (!isAdmin && (auction.Listing == null || auction.Listing.SellerId != userId)) 
                return Forbid();

            if (auction.EndTime <= now)
            {
                TempData["Error"] = "This auction has ended and can’t be edited.";
                return isAdmin
                    ? RedirectToAction("Dashboard", "Admin")
                    : RedirectToAction("Dashboard", "Seller");
            }

            ViewData["IsGroup"] = false;
            ViewData["AuctionId"] = id;
            return View("~/Views/Auctions/Edit.cshtml");
        }

        [HttpGet("/Auctions/Detail/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var group = await _auctionService.GetAuctionGroupByIdAsync(id);
            if (group != null && group.Auctions.Count > 0)
            {
                await SetWatchlistViewBag();
                return View("~/Views/Auctions/GroupDetail.cshtml", group);
            }

            var auction = await _auctionService.GetAuctionByIdAsync(id);
            if (auction == null)
                return NotFound();

            var wrapper = new AuctionGroupDetailDto
            {
                AuctionGroupId = auction.AuctionGroupId ?? auction.AuctionId,
                Title = auction.AuctionGroupTitle ?? auction.Title,
                CreatedAt = auction.StartTime,
                Auctions = new List<AuctionBrowseDto> { auction }
            };

            await SetWatchlistViewBag();
            return View("~/Views/Auctions/GroupDetail.cshtml", wrapper);
        }

        [HttpGet("/Auctions/Auction/{id:int}")]
        public IActionResult Auction(int id)
        {
            ViewData["AuctionId"] = id;
            return View("~/Views/Auctions/Auction.cshtml");
        }

        [HttpGet("/Auctions/Delete/{id:int}")]
        [HttpPost("/Auctions/Delete/{id:int}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                return Forbid();

            var isAdmin = User.IsInRole("Admin");

            // First, try to find an auction group with this ID and delete all its auctions
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

                    // Verify all auctions belong to this seller (or user is admin)
                    if (!isAdmin && auctions.Any(a => a.Listing == null || a.Listing.SellerId != userId))
                        return Forbid();

                    // Delete bids first
                    var bids = await _db.Bids.Where(b => auctionIds.Contains(b.AuctionId)).ToListAsync();
                    _db.Bids.RemoveRange(bids);

                    // Delete group joins
                    _db.AuctionGroupAuctions.RemoveRange(groupJoins);

                    // Delete auctions
                    _db.Auctions.RemoveRange(auctions);

                    // Delete the group itself
                    var group = await _db.AuctionGroups.FindAsync(id);
                    if (group != null)
                        _db.AuctionGroups.Remove(group);

                    // Delete listings
                    var listings = auctions.Where(a => a.Listing != null).Select(a => a.Listing!).ToList();
                    _db.Listings.RemoveRange(listings);

                    await _db.SaveChangesAsync();
                    return isAdmin ? RedirectToAction("Dashboard", "Admin") : RedirectToAction("Dashboard", "Seller");
                }
            }
            catch
            {
                // AuctionGroup tables may not exist; fall through to single-auction lookup
            }

            // Fall back to deleting a single auction by its AuctionId
            var singleAuction = await _db.Auctions
                .Include(a => a.Listing)
                .FirstOrDefaultAsync(a => a.AuctionId == id);

            if (singleAuction == null)
                return NotFound();

            // Allow if admin or if user owns the auction
            if (!isAdmin && (singleAuction.Listing == null || singleAuction.Listing.SellerId != userId))
                return Forbid();

            // Delete bids first
            var singleBids = await _db.Bids.Where(b => b.AuctionId == id).ToListAsync();
            _db.Bids.RemoveRange(singleBids);

            // Delete auction
            _db.Auctions.Remove(singleAuction);

            // Delete listing
            if (singleAuction.Listing != null)
                _db.Listings.Remove(singleAuction.Listing);

            await _db.SaveChangesAsync();

            return isAdmin ? RedirectToAction("Dashboard", "Admin") : RedirectToAction("Dashboard", "Seller");
        }

        private async Task SetWatchlistViewBag()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            ViewBag.IsAuthenticated = isAuthenticated;
            ViewBag.WatchlistIds = new HashSet<int>();

            if (!isAuthenticated) return;

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return;

            var ids = await _db.Favourites
                .AsNoTracking()
                .Where(f => f.UserId == user.Id)
                .Select(f => f.AuctionId)
                .ToListAsync();

            ViewBag.WatchlistIds = new HashSet<int>(ids);
        }
    }
}
