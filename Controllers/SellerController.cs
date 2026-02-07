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

                // Build grouping by querying AuctionGroupAuctions joined to Auctions for this seller
                var auctionGroupsVm = new List<AuctionGroupViewModel>();
                try
                {
                    var groupJoins = await _db.AuctionGroupAuctions
                        .AsNoTracking()
                        .Include(aga => aga.Auction!)
                            .ThenInclude(a => a.Listing)
                        .Where(aga => aga.Auction != null && aga.Auction.Listing != null && aga.Auction.Listing.SellerId == sellerId)
                        .ToListAsync();

                    var groupedAuctionIds = groupJoins.Select(j => j.AuctionId).Distinct().ToHashSet();
                    var groupIds = groupJoins.Select(j => j.AuctionGroupId).Distinct().ToList();
                    var groups = await _db.AuctionGroups.AsNoTracking().Where(g => groupIds.Contains(g.AuctionGroupId)).ToListAsync();

                    // For each groupId, collect auctions (from the full auctions list) that belong to it
                    foreach (var gid in groupIds)
                    {
                        var memberAuctionIds = groupJoins.Where(j => j.AuctionGroupId == gid).Select(j => j.AuctionId).ToList();
                        var members = auctions.Where(a => memberAuctionIds.Contains(a.AuctionId)).ToList();
                        var g = groups.FirstOrDefault(x => x.AuctionGroupId == gid);

                        if (members.Count > 0)
                        {
                            auctionGroupsVm.Add(new AuctionGroupViewModel
                            {
                                GroupKey = gid,
                                AuctionGroupId = gid,
                                Title = g?.Title ?? "Group",
                                Auctions = members
                            });
                        }
                    }

                    // Add any ungrouped auctions as individual groups
                    var ungrouped = auctions.Where(a => !groupedAuctionIds.Contains(a.AuctionId)).ToList();
                    foreach (var a in ungrouped)
                    {
                        auctionGroupsVm.Add(new AuctionGroupViewModel
                        {
                            GroupKey = a.AuctionId,
                            AuctionGroupId = null,
                            Title = a.Listing?.Title ?? $"Listing #{a.ListingId}",
                            Auctions = new List<Auction> { a }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AuctionGroup tables unavailable when building seller dashboard; falling back to individual auctions.");
                    // Fallback: show each auction as its own group
                    foreach (var a in auctions)
                    {
                        auctionGroupsVm.Add(new AuctionGroupViewModel
                        {
                            GroupKey = a.AuctionId,
                            AuctionGroupId = null,
                            Title = a.Listing?.Title ?? $"Listing #{a.ListingId}",
                            Auctions = new List<Auction> { a }
                        });
                    }
                }

                // Compute auction stats (bids, revenue, needs attention)
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

                // After building auctionGroupsVm, log summary for debugging
                try
                {
                    _logger.LogInformation("SellerDashboard: built {GroupCount} auction groups for seller {SellerId}", auctionGroupsVm.Count, sellerId);
                    foreach (var gvm in auctionGroupsVm)
                    {
                        var memberIds = string.Join(',', gvm.Auctions.Select(a => a.AuctionId));
                        _logger.LogInformation("Group {GroupKey} (GroupId={GroupId}) has {Count} auctions: {MemberIds}", gvm.GroupKey, gvm.AuctionGroupId.HasValue ? gvm.AuctionGroupId.ToString() : "(none)", gvm.Auctions.Count, memberIds);
                    }
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Failed to log auction group summary");
                }

                var viewModel = new SellerDashboardViewModel
                {
                    Auctions = auctions,
                    AuctionGroups = auctionGroupsVm,
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAuction(int auctionId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return RedirectToAction("Login", "Auth");

                var sellerId = user.Id;
                var now = DateTime.UtcNow;

                // Load auction with listing to verify ownership
                var auction = await _db.Auctions
                    .Include(a => a.Listing)
                    .FirstOrDefaultAsync(a => a.AuctionId == auctionId);

                if (auction == null || auction.Listing == null || auction.Listing.SellerId != sellerId)
                {
                    TempData["Error"] = "Auction not found or you do not have permission to delete it.";
                    return RedirectToAction("Dashboard");
                }

                if (auction.EndTime > now)
                {
                    TempData["Error"] = "You can only delete ended auctions.";
                    return RedirectToAction("Dashboard");
                }

                // Delete dependent records in correct order to respect FK constraints

                // 1. Notifications referencing this auction
                var notifications = await _db.Notifications
                    .Where(n => n.AuctionId == auctionId)
                    .ToListAsync();
                _db.Notifications.RemoveRange(notifications);

                // 2. Payments
                var payments = await _db.Payments
                    .Where(p => p.AuctionId == auctionId)
                    .ToListAsync();
                _db.Payments.RemoveRange(payments);

                // 3. AuctionResult (references WinningBidId, so must be deleted before bids)
                var auctionResult = await _db.AuctionResults
                    .FirstOrDefaultAsync(ar => ar.AuctionId == auctionId);
                if (auctionResult != null)
                    _db.AuctionResults.Remove(auctionResult);

                // 4. Bids (notifications referencing bids are nullable AuctionId-linked, already cleared above)
                var bids = await _db.Bids
                    .Where(b => b.AuctionId == auctionId)
                    .ToListAsync();
                _db.Bids.RemoveRange(bids);

                // 5. AuctionGroupAuction join entries
                try
                {
                    var groupJoins = await _db.AuctionGroupAuctions
                        .Where(aga => aga.AuctionId == auctionId)
                        .ToListAsync();
                    _db.AuctionGroupAuctions.RemoveRange(groupJoins);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not clean up AuctionGroupAuctions for auction {AuctionId}", auctionId);
                }

                // 6. The auction itself
                _db.Auctions.Remove(auction);

                await _db.SaveChangesAsync();

                _logger.LogInformation("Seller {SellerId} deleted ended auction {AuctionId}", sellerId, auctionId);
                TempData["Success"] = "Auction deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete auction {AuctionId}", auctionId);
                TempData["Error"] = "Failed to delete auction. Please try again.";
            }

            return RedirectToAction("Dashboard");
        }
    }
}
