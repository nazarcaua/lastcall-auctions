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
    }
}
