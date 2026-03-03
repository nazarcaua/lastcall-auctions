using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.Models;
using LastCallMotorAuctions.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProfileController> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public ProfileController(
            ApplicationDbContext db,
            UserManager<User> userManager,
            ILogger<ProfileController> logger,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
            _env = env;
        }

        [HttpGet("Profile/{id:int}")]
        public async Task<IActionResult> Index(int id)
        {
            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Status)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var isSeller = roles.Contains("Seller");
            var isBuyer = roles.Contains("Buyer");

            var currentUser = await _userManager.GetUserAsync(User);
            var isOwnProfile = currentUser != null && currentUser.Id == id;

            var vm = new ProfileViewModel
            {
                UserId = user.Id,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                EmailConfirmed = user.EmailConfirmed,
                ProfilePictureUrl = user.ProfilePictureUrl,
                StatusId = user.StatusId,
                StatusName = user.Status?.Name ?? (user.StatusId == 1 ? "Active" : "Suspended"),
                Roles = roles.ToList(),
                IsOwnProfile = isOwnProfile,
                IsSeller = isSeller,
                IsBuyer = isBuyer
            };

            // If seller, load their auction groups (active + recently ended)
            if (isSeller)
            {
                var now = DateTime.UtcNow;

                var auctions = await _db.Auctions
                    .AsNoTracking()
                    .Include(a => a.Listing).ThenInclude(l => l!.Make)
                    .Include(a => a.Listing).ThenInclude(l => l!.Model)
                    .Include(a => a.Status)
                    .Where(a => a.Listing != null && a.Listing.SellerId == id)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                // Build auction groups (same pattern as SellerController)
                var auctionGroupsVm = new List<AuctionGroupViewModel>();

                var groupJoins = await _db.AuctionGroupAuctions
                    .AsNoTracking()
                    .Include(aga => aga.Auction!).ThenInclude(a => a.Listing)
                    .Where(aga => aga.Auction != null && aga.Auction.Listing != null && aga.Auction.Listing.SellerId == id)
                    .ToListAsync();

                var groupedAuctionIds = groupJoins.Select(j => j.AuctionId).Distinct().ToHashSet();
                var groupIds = groupJoins.Select(j => j.AuctionGroupId).Distinct().ToList();
                var groups = await _db.AuctionGroups.AsNoTracking().Where(g => groupIds.Contains(g.AuctionGroupId)).ToListAsync();

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

                vm.ActiveAuctionGroups = auctionGroupsVm
                    .Where(g => g.Auctions.Any(a => a.EndTime > now))
                    .OrderBy(g => g.Auctions.Min(a => a.EndTime))
                    .ToList();

                vm.RecentlyEndedAuctions = auctions
                    .Where(a => a.EndTime <= now)
                    .OrderByDescending(a => a.EndTime)
                    .Take(10)
                    .ToList();
            }

            return View(vm);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Auth");

            await _db.Entry(user).Reference(u => u.Status).LoadAsync();
            var roles = await _userManager.GetRolesAsync(user);

            var vm = new EditProfileViewModel
            {
                UserId = user.Id,
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                EmailConfirmed = user.EmailConfirmed,
                ProfilePictureUrl = user.ProfilePictureUrl,
                StatusId = user.StatusId,
                StatusName = user.Status?.Name ?? (user.StatusId == 1 ? "Active" : "Suspended"),
                Roles = roles.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string fullName, string email, IFormFile? profilePicture, string? profilePictureUrl)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Auth");

            // Update full name
            if (!string.IsNullOrWhiteSpace(fullName) && fullName != user.FullName)
            {
                user.FullName = fullName.Trim();
            }

            // Update email
            if (!string.IsNullOrWhiteSpace(email) && email != user.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    TempData["Error"] = "That email address is already in use.";
                    return RedirectToAction("Edit");
                }

                user.Email = email.Trim();
                user.NormalizedEmail = email.Trim().ToUpperInvariant();
                user.UserName = email.Trim();
                user.NormalizedUserName = email.Trim().ToUpperInvariant();
                user.EmailConfirmed = false; // Reset confirmation when email changes
            }

            // Handle profile picture: file upload takes priority over URL
            if (profilePicture != null && profilePicture.Length > 0)
            {
                if (profilePicture.Length > MaxFileSize)
                {
                    TempData["Error"] = "Profile picture exceeds the 5 MB limit.";
                    return RedirectToAction("Edit");
                }

                var ext = Path.GetExtension(profilePicture.FileName);
                if (!AllowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "File type not allowed. Use .jpg, .jpeg, .png, or .webp.";
                    return RedirectToAction("Edit");
                }

                try
                {
                    var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "profiles", user.Id.ToString());
                    Directory.CreateDirectory(uploadDir);

                    // Delete old profile picture files
                    foreach (var file in Directory.GetFiles(uploadDir))
                        System.IO.File.Delete(file);

                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }

                    user.ProfilePictureUrl = $"/uploads/profiles/{user.Id}/{fileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save profile picture for user {UserId}", user.Id);
                    TempData["Error"] = "Failed to save the profile picture. Please try again.";
                    return RedirectToAction("Edit");
                }
            }
            else if (!string.IsNullOrWhiteSpace(profilePictureUrl))
            {
                // Use the provided URL (must start with http:// or https://)
                var trimmed = profilePictureUrl.Trim();
                if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    user.ProfilePictureUrl = trimmed;
                }
                else
                {
                    TempData["Error"] = "Image URL must start with http:// or https://.";
                    return RedirectToAction("Edit");
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} updated their profile", user.Id);
                TempData["Success"] = "Profile updated successfully.";
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Failed to update profile: {errors}";
            }

            return RedirectToAction("Edit");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProfilePicture()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Auth");

            if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl.StartsWith("/uploads/"))
            {
                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "profiles", user.Id.ToString());
                if (Directory.Exists(uploadDir))
                {
                    foreach (var file in Directory.GetFiles(uploadDir))
                        System.IO.File.Delete(file);
                }
            }

            user.ProfilePictureUrl = null;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Profile picture removed.";
            return RedirectToAction("Edit");
        }
    }
}
