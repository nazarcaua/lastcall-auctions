using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.Models;
using LastCallMotorAuctions.API.Services;
using LastCallMotorAuctions.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AdminController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public AdminController(
            ApplicationDbContext db,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<AdminController> logger,
            INotificationService notificationService,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _notificationService = notificationService;
            _env = env;
        }

        private List<string> GetPhotoUrlsForListing(int listingId)
        {
            var uploadDir = Path.Combine(_env.WebRootPath ?? "", "uploads", "listings", listingId.ToString());
            if (!Directory.Exists(uploadDir))
                return new List<string>();

            return Directory.GetFiles(uploadDir)
                .Where(f => AllowedPhotoExtensions.Contains(Path.GetExtension(f)))
                .OrderBy(f => f)
                .Select(f => $"/uploads/listings/{listingId}/{Path.GetFileName(f)}")
                .ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var now = DateTime.UtcNow;

                // Load all users with status
                var users = await _db.Users
                    .AsNoTracking()
                    .Include(u => u.Status)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                var userViewModels = new List<AdminUserViewModel>();
                foreach (var u in users)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    userViewModels.Add(new AdminUserViewModel
                    {
                        UserId = u.Id,
                        FullName = u.FullName ?? "",
                        Email = u.Email ?? "",
                        StatusId = u.StatusId,
                        StatusName = u.Status?.Name ?? $"StatusId: {u.StatusId}",
                        Roles = roles.ToList(),
                        LockoutEnd = u.LockoutEnd?.UtcDateTime
                    });
                }

                // Load ALL auctions for admin management
                var auctions = await _db.Auctions
                    .AsNoTracking()
                    .Include(a => a.Listing).ThenInclude(l => l!.Seller)
                    .Include(a => a.Status)
                    .OrderByDescending(a => a.EndTime > now) // Active first
                    .ThenBy(a => a.EndTime)
                    .ToListAsync();

                var auctionIds = auctions.Select(a => a.AuctionId).ToList();

                // Get bid counts and highest bids
                var bidStats = await _db.Bids
                    .Where(b => auctionIds.Contains(b.AuctionId))
                    .GroupBy(b => b.AuctionId)
                    .Select(g => new { AuctionId = g.Key, Count = g.Count(), MaxBid = g.Max(b => b.Amount) })
                    .ToDictionaryAsync(x => x.AuctionId, x => new { x.Count, x.MaxBid });

                // Get auction group info
                var auctionGroups = await _db.AuctionGroupAuctions
                    .Include(aga => aga.AuctionGroup)
                    .Where(aga => auctionIds.Contains(aga.AuctionId))
                    .ToDictionaryAsync(aga => aga.AuctionId, aga => aga);

                var auctionViewModels = auctions.Select(a => new AdminAuctionViewModel
                {
                    AuctionId = a.AuctionId,
                    AuctionGroupId = auctionGroups.GetValueOrDefault(a.AuctionId)?.AuctionGroupId,
                    AuctionGroupTitle = auctionGroups.GetValueOrDefault(a.AuctionId)?.AuctionGroup?.Title,
                    Title = a.Listing?.Title ?? "Unknown",
                    SellerId = a.Listing?.SellerId ?? 0,
                    SellerName = a.Listing?.Seller?.FullName ?? "Unknown",
                    SellerEmail = a.Listing?.Seller?.Email ?? "",
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    StatusId = a.StatusId,
                    StatusName = a.Status?.Name ?? "Unknown",
                    StartPrice = a.StartPrice,
                    CurrentBid = bidStats.GetValueOrDefault(a.AuctionId)?.MaxBid,
                    BidCount = bidStats.GetValueOrDefault(a.AuctionId)?.Count ?? 0,
                    HasStarted = a.StartTime <= now,
                    HasEnded = a.EndTime <= now,
                    PhotoUrl = a.Listing != null ? GetPhotoUrlsForListing(a.ListingId).FirstOrDefault() : null
                }).ToList();

                // Gather stats
                var totalAuctions = auctions.Count;
                var activeAuctions = auctions.Count(a => a.EndTime > now && a.StartTime <= now);
                var totalListings = await _db.Listings.AsNoTracking().CountAsync();

                var viewModel = new AdminDashboardViewModel
                {
                    Users = userViewModels,
                    TotalUsers = userViewModels.Count,
                    ActiveUsers = userViewModels.Count(u => u.StatusId == 1),
                    SuspendedUsers = userViewModels.Count(u => u.StatusId == 2),
                    BuyerCount = userViewModels.Count(u => u.Roles.Contains("Buyer")),
                    SellerCount = userViewModels.Count(u => u.Roles.Contains("Seller")),
                    AdminCount = userViewModels.Count(u => u.Roles.Contains("Admin")),
                    TotalAuctions = totalAuctions,
                    ActiveAuctions = activeAuctions,
                    TotalListings = totalListings,
                    Auctions = auctionViewModels
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Admin Dashboard");
                ViewBag.DbError = "Failed to load dashboard data.";
                return View(new AdminDashboardViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantSeller(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Dashboard");
            }

            if (await _userManager.IsInRoleAsync(user, "Seller"))
            {
                TempData["Error"] = $"{user.FullName} is already a Seller.";
                return RedirectToAction("Dashboard");
            }

            var result = await _userManager.AddToRoleAsync(user, "Seller");
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin granted Seller role to user {UserId} ({Email})", user.Id, user.Email);
                TempData["Success"] = $"{user.FullName} has been upgraded to Seller.";

                await _notificationService.CreateAsync(user.Id, "SellerRequestApproved", "Seller request approved", "Your request to become a seller has been approved. Please log out and log back in to access seller features.");

                // If the admin granted the role to themselves, refresh their sign-in to update claims
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id == user.Id)
                {
                    await _signInManager.RefreshSignInAsync(currentUser);
                }
                else
                {
                    // Force other users to re-login by updating their security stamp
                    await _userManager.UpdateSecurityStampAsync(user);
                }
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Failed to grant Seller role: {errors}";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeSeller(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Dashboard");
            }

            if (!await _userManager.IsInRoleAsync(user, "Seller"))
            {
                TempData["Error"] = $"{user.FullName} is not a Seller.";
                return RedirectToAction("Dashboard");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "Seller");
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin revoked Seller role from user {UserId} ({Email})", user.Id, user.Email);
                TempData["Success"] = $"Seller role removed from {user.FullName}.";

                await _notificationService.CreateAsync(user.Id, "SellerRequestRejected", "Seller access revoked", "Your Seller access has been removed.");

                // If the admin revoked the role from themselves, refresh their sign-in to update claims
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null && currentUser.Id == user.Id)
                {
                    await _signInManager.RefreshSignInAsync(currentUser);
                }
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Failed to revoke Seller role: {errors}";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(int userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == userId)
            {
                TempData["Error"] = "You cannot suspend your own account.";
                return RedirectToAction("Dashboard");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Dashboard");
            }

            user.StatusId = 2; // Suspended
            await _userManager.UpdateAsync(user);

            // Also lock out the user so existing sessions are invalidated
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

            _logger.LogInformation("Admin suspended user {UserId} ({Email})", user.Id, user.Email);
            TempData["Success"] = $"{user.FullName} has been suspended.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateUser(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Dashboard");
            }

            user.StatusId = 1; // Active
            await _userManager.UpdateAsync(user);

            // Remove lockout
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            _logger.LogInformation("Admin activated user {UserId} ({Email})", user.Id, user.Email);
            TempData["Success"] = $"{user.FullName} has been activated.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == userId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Dashboard");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                // Delete user's notifications
                var notifications = await _db.Notifications.Where(n => n.UserId == userId).ToListAsync();
                _db.Notifications.RemoveRange(notifications);

                // Delete user's bids (must remove auction results referencing those bids first)
                var bidIds = await _db.Bids.Where(b => b.BidderId == userId).Select(b => b.BidId).ToListAsync();
                if (bidIds.Count > 0)
                {
                    var auctionResults = await _db.AuctionResults.Where(ar => bidIds.Contains(ar.WinningBidId)).ToListAsync();
                    _db.AuctionResults.RemoveRange(auctionResults);

                    var bids = await _db.Bids.Where(b => b.BidderId == userId).ToListAsync();
                    _db.Bids.RemoveRange(bids);
                }

                // Delete seller's listings/auctions chain
                var sellerListingIds = await _db.Listings.Where(l => l.SellerId == userId).Select(l => l.ListingId).ToListAsync();
                if (sellerListingIds.Count > 0)
                {
                    var auctionIds = await _db.Auctions.Where(a => sellerListingIds.Contains(a.ListingId)).Select(a => a.AuctionId).ToListAsync();

                    if (auctionIds.Count > 0)
                    {
                        // Payments
                        var payments = await _db.Payments.Where(p => auctionIds.Contains(p.AuctionId)).ToListAsync();
                        _db.Payments.RemoveRange(payments);

                        // Auction results
                        var results = await _db.AuctionResults.Where(ar => auctionIds.Contains(ar.AuctionId)).ToListAsync();
                        _db.AuctionResults.RemoveRange(results);

                        // Bids on those auctions
                        var auctionBids = await _db.Bids.Where(b => auctionIds.Contains(b.AuctionId)).ToListAsync();
                        _db.Bids.RemoveRange(auctionBids);

                        // Auction group joins
                        var groupJoins = await _db.AuctionGroupAuctions.Where(aga => auctionIds.Contains(aga.AuctionId)).ToListAsync();
                        _db.AuctionGroupAuctions.RemoveRange(groupJoins);

                        // Notifications referencing those auctions
                        var auctionNotifications = await _db.Notifications.Where(n => n.AuctionId.HasValue && auctionIds.Contains(n.AuctionId.Value)).ToListAsync();
                        _db.Notifications.RemoveRange(auctionNotifications);

                        // Auctions
                        var auctions = await _db.Auctions.Where(a => auctionIds.Contains(a.AuctionId)).ToListAsync();
                        _db.Auctions.RemoveRange(auctions);
                    }

                    // Listings
                    var listings = await _db.Listings.Where(l => sellerListingIds.Contains(l.ListingId)).ToListAsync();
                    _db.Listings.RemoveRange(listings);
                }

                await _db.SaveChangesAsync();

                // Finally delete the user via Identity
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin deleted user {UserId} ({Email})", userId, user.Email);
                    TempData["Success"] = $"User {user.FullName} has been permanently deleted.";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Failed to delete user: {errors}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                TempData["Error"] = "An error occurred while deleting the user. Some related data may have foreign key constraints.";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantAdmin(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Dashboard");
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = $"{user.FullName} is already an Admin.";
                return RedirectToAction("Dashboard");
            }

            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin granted Admin role to user {UserId} ({Email})", user.Id, user.Email);
                TempData["Success"] = $"{user.FullName} has been granted Admin access.";
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Failed to grant Admin role: {errors}";
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAdmin(int userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == userId)
            {
                TempData["Error"] = "You cannot revoke your own Admin access.";
                return RedirectToAction("Dashboard");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Dashboard");
            }

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = $"{user.FullName} is not an Admin.";
                return RedirectToAction("Dashboard");
            }

            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin revoked Admin role from user {UserId} ({Email})", user.Id, user.Email);
                TempData["Success"] = $"Admin role removed from {user.FullName}.";
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Failed to revoke Admin role: {errors}";
            }

            return RedirectToAction("Dashboard");
        }
    }
}
