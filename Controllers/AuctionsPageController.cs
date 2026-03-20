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

            var singleSellerId = singleAuction.Listing.SellerId;
            await _notificationService.CreateAsync(singleSellerId, "AuctionEnded", "Auction ended", "Your auction has ended.", auctionId: singleAuction.AuctionId);
            var singleWinnerId = await _db.Bids
                .Where(b => b.AuctionId == singleAuction.AuctionId)
                .OrderByDescending(b => b.Amount)
                .Select(b => b.BidderId)
                .FirstOrDefaultAsync();
            if (singleWinnerId != 0)
                await _notificationService.CreateAsync(singleWinnerId, "AuctionWon", "You won the auction", "You had the highest bid when the auction ended.", auctionId: singleAuction.AuctionId);

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
