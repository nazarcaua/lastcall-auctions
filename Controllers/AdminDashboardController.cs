using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/admin/dashboard")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminDashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<AdminDashboardSummaryDto>> GetSummary()
        {
            // Live auctions = AuctionStatus.Name == "Active"
            var liveStatus = await _db.AuctionStatuses
                .Where(s => s.Name == "Active")
                .Select(s => s.StatusId)
                .SingleAsync();

            var liveAuctionCount = await _db.Auctions
                .CountAsync(a => a.StatusId == liveStatus);

            var dto = new AdminDashboardSummaryDto
            {
                PendingSellerRequests = 0, // placeholder
                PendingListings = 0, // placeholder
                LiveAuctions = liveAuctionCount
            };

            return Ok(dto);
        }

        [HttpGet("live-auctions")]
        public async Task<ActionResult<List<AdminLiveAuctionDto>>> GetLiveAuction()
        {
            // StatusId for "Active" auctions
            var activeStatusId = await _db.AuctionStatuses
                .Where(s => s.Name == "Active")
                .Select (s => s.StatusId)
                .SingleAsync();

            var now = DateTime.UtcNow;
            // Last 5 mins for bid rate/active users
            var windowStart = now.AddMinutes(-5);

            var liveAuctions = await _db.Auctions
                .Where(a => a.StatusId == activeStatusId)
                .Select(a => new AdminLiveAuctionDto
                {
                    AuctionId = a.AuctionId,
                    VehicleTitle = a.Listing!.Title,
                    CurrentBid = _db.Bids
                        .Where(b => b.AuctionId == a.AuctionId)
                        .Select(b => (decimal?)b.Amount)
                        .OrderByDescending(b => b)
                        .FirstOrDefault() ?? a.StartPrice,
                    ActiveUsers = _db.Bids
                        .Where(b => b.AuctionId == a.AuctionId && b.PlacedAt >= windowStart)
                        .Select(b => b.BidderId)
                        .Distinct()
                        .Count(),
                    BidsPerMinute = (decimal)_db.Bids
                        .Where(b => b.AuctionId == a.AuctionId && b.PlacedAt >= windowStart)
                        .Count()
                })
                .ToListAsync();

            return Ok(liveAuctions);
        }
    }
}
