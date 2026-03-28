using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Services;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using LastCallMotorAuctions.API.Hubs;

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
        private readonly IHubContext<BiddingHub> _hubContext;

        public AuctionsController(IAuctionService auctionService, ILogger<AuctionsController> logger, ApplicationDbContext db, IWebHostEnvironment env, IHubContext<BiddingHub> hubContext)
        {
            _auctionService = auctionService;
            _logger = logger;
            _db = db;
            _env = env;
            _hubContext = hubContext;
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
        [Authorize(Roles = "Admin")]
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

                if (auction.Listing != null && auction.Listing.SellerId == userId)
                    return BadRequest(new { message = "You cannot bid on your own auction." });

                if (auction.Listing == null || auction.Listing.SellerId != userId)
                    return Forbid();

                // Validate EndTime if provided
                if (dto.EndTime.HasValue)
                {
                    var endUtc = dto.EndTime.Value.ToUniversalTime();
                    var now = DateTime.UtcNow;
                    if (auction.EndTime <= now)
                        return BadRequest(new { message = "This auction has ended and can’t be updated." });
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

        /// <summary>
        /// Get bid history for an auction
        /// </summary>
        [HttpGet("{id:int}/bids")]
        public async Task<IActionResult> GetBids(int id)
        {
            var bids = await _db.Bids
                .AsNoTracking()
                .Where(b => b.AuctionId == id)
                .OrderByDescending(b => b.Amount)
                .Select(b => new BidResponseDto
                {
                    BidId = b.BidId,
                    AuctionId = b.AuctionId,
                    BidderId = b.BidderId,
                    BidderName = b.Bidder != null ? b.Bidder.FullName : "Anonymous",
                    Amount = b.Amount,
                    PlacedAt = b.PlacedAt
                })
                .ToListAsync();

            return Ok(bids);
        }

        /// <summary>
        /// Place a bid on an auction (any authenticated user)
        /// </summary>
        [HttpPost("{id:int}/bids")]
        [Authorize]
        public async Task<IActionResult> PlaceBid(int id, [FromBody] PlaceBidDto dto)
        {
            try
            {
                var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                    return Unauthorized(new { message = "User not authenticated." });

                var auction = await _db.Auctions
                    .Include(a => a.Listing)
                    .FirstOrDefaultAsync(a => a.AuctionId == id);

                if (auction == null)
                    return NotFound(new { message = $"Auction with ID {id} not found." });

                // Check if auction has ended
                if (auction.EndTime <= DateTime.UtcNow)
                    return BadRequest(new { message = "This auction has ended." });

                // Check if auction has started
                if (auction.StartTime > DateTime.UtcNow)
                    return BadRequest(new { message = "This auction has not started yet." });

                // Check if user is the last bidder (no consecutive bids allowed)
                var lastBid = await _db.Bids
                    .Where(b => b.AuctionId == id)
                    .OrderByDescending(b => b.PlacedAt)
                    .FirstOrDefaultAsync();

                if (lastBid != null && lastBid.BidderId == userId)
                    return BadRequest(new { message = "You cannot place consecutive bids. Please wait for another bidder." });

                // Get current highest bid
                var currentHighestBid = await _db.Bids
                    .Where(b => b.AuctionId == id)
                    .MaxAsync(b => (decimal?)b.Amount) ?? 0;

                var minimumBid = currentHighestBid > 0 ? currentHighestBid + 100 : auction.StartPrice;

                if (dto.Amount < minimumBid)
                    return BadRequest(new { message = $"Bid must be at least ${minimumBid:N0}." });

                // Create the bid
                var bid = new Bid
                {
                    AuctionId = id,
                    BidderId = userId,
                    Amount = dto.Amount,
                    PlacedAt = DateTime.UtcNow
                };

                _db.Bids.Add(bid);
                await _db.SaveChangesAsync();

                // Get bidder name for response
                var bidder = await _db.Users.FindAsync(userId);
                var bidderName = bidder?.FullName ?? "Anonymous";

                var response = new BidResponseDto
                {
                    BidId = bid.BidId,
                    AuctionId = bid.AuctionId,
                    BidderId = bid.BidderId,
                    BidderName = bidderName,
                    Amount = bid.Amount,
                    PlacedAt = bid.PlacedAt
                };

                // Broadcast to SignalR clients
                await _hubContext.Clients.Group($"auction-{id}").SendAsync("NewBid", response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing bid on auction {AuctionId}", id);
                return StatusCode(500, new { message = "Failed to place bid. Please try again." });
            }
        }

        /// <summary>
        /// Toggle favorite/watchlist for an auction (any authenticated user)
        /// </summary>
        [HttpPost("{id:int}/favorite")]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            _logger.LogInformation("ToggleFavorite called for auction {AuctionId}", id);

            try
            {
                var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation("User claim: {Claim}", claim ?? "null");

                if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var userId))
                {
                    _logger.LogWarning("User not authenticated or invalid claim");
                    return Unauthorized(new { message = "User not authenticated." });
                }

                var auctionExists = await _db.Auctions.AnyAsync(a => a.AuctionId == id);
                if (!auctionExists)
                {
                    _logger.LogWarning("Auction {AuctionId} not found", id);
                    return NotFound(new { message = $"Auction with ID {id} not found." });
                }

                var existing = await _db.Favourites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.AuctionId == id);

                bool isFavourited;
                if (existing != null)
                {
                    _db.Favourites.Remove(existing);
                    await _db.SaveChangesAsync();
                    isFavourited = false;
                    _logger.LogInformation("Removed favorite for user {UserId} auction {AuctionId}", userId, id);
                }
                else
                {
                    _db.Favourites.Add(new Favourite { UserId = userId, AuctionId = id });
                    await _db.SaveChangesAsync();
                    isFavourited = true;
                    _logger.LogInformation("Added favorite for user {UserId} auction {AuctionId}", userId, id);
                }

                return Ok(new { isFavourited });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite for auction {AuctionId}", id);
                return StatusCode(500, new { message = "Failed to update favorite. Please try again.", error = ex.Message });
            }
        }
    }
}
