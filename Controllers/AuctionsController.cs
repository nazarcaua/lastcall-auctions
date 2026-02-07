using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Services;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionsController : ControllerBase
    {
        private readonly IAuctionService _auctionService;
        private readonly ILogger<AuctionsController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AuctionsController(IAuctionService auctionService, ILogger<AuctionsController> logger, ApplicationDbContext db, IWebHostEnvironment env)
        {
            _auctionService = auctionService;
            _logger = logger;
            _db = db;
            _env = env;
        }

        /// Get all active auctions (for browse list).
        [HttpGet]
        public async Task<IActionResult> GetActiveAuctions()
        {
            var list = await _auctionService.GetActiveAuctionAsync();
            return Ok(list);
        }

        /// Debug: return recent raw auctions (includes StatusId, StartTime, EndTime)
        [HttpGet("debug/recent")]
        public async Task<IActionResult> GetRecentAuctionsDebug()
        {
            var recent = await _db.Auctions
                .AsNoTracking()
                .OrderByDescending(a => a.AuctionId)
                .Take(50)
                .Select(a => new
                {
                    a.AuctionId,
                    a.ListingId,
                    a.StartPrice,
                    a.ReservePrice,
                    a.StartTime,
                    a.EndTime,
                    a.StatusId
                })
                .ToListAsync();

            return Ok(recent);
        }

        /// Get a single auction by ID (for detail page).
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAuction(int id)
        {
            var auction = await _auctionService.GetAuctionByIdAsync(id);
            if (auction == null)
                return NotFound(new { message = "Auction not found." });
            return Ok(auction);
        }

        /// Update auction (seller only)
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateAuction(int id, [FromBody] UpdateAuctionDto dto)
        {
            try
            {
                var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                    return Unauthorized();

                var auction = await _db.Auctions
                    .Include(a => a.Listing)
                    .FirstOrDefaultAsync(a => a.AuctionId == id);

                if (auction == null)
                    return NotFound(new { message = "Auction not found." });

                if (auction.Listing == null || auction.Listing.SellerId != userId)
                    return Forbid();

                // Validate EndTime if provided
                if (dto.EndTime.HasValue)
                {
                    var endUtc = dto.EndTime.Value.ToUniversalTime();
                    var now = DateTime.UtcNow;
                    if (endUtc <= now.AddMinutes(1))
                        return BadRequest(new { message = "Auction end time must be at least 1 minute in the future." });
                    auction.EndTime = endUtc;
                }

                if (dto.StartPrice.HasValue)
                    auction.StartPrice = dto.StartPrice.Value;
                if (dto.ReservePrice.HasValue)
                    auction.ReservePrice = dto.ReservePrice;

                await _db.SaveChangesAsync();

                return Ok(new { message = "Auction updated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating auction {AuctionId}", id);
                if (_env.IsDevelopment())
                    return StatusCode(500, new { message = "An error occurred.", exception = ex.GetType().FullName, details = ex.ToString() });
                return StatusCode(500, new { message = "An error occurred." });
            }
        }
    }
}
