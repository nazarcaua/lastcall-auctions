using LastCallMotorAuctions.API.Services;
using LastCallMotorAuctions.API.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace LastCallMotorAuctions.API.Controllers
{
    [Route("api/buyer")]
    [ApiController]
    [Authorize(Roles = "Buyer")]
    public class BuyerController : ControllerBase
    {
        private readonly IAuctionService _auctionService;

        public BuyerController(IAuctionService auctionService)
        {
            _auctionService = auctionService;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<BuyerDashboardDto>> GetDashboard()
        {
            var buyerIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (buyerIdClaim == null || !int.TryParse(buyerIdClaim.Value, out var buyerId))
                return Unauthorized("Buyer ID not found in token.");

            try
            {
                var dashboard = await _auctionService.GetBuyerDashboardAsync(buyerId);

                var dto = new BuyerDashboardDto
                {
                    BuyerId = dashboard.BuyerId,
                    BuyerName = dashboard.BuyerName,
                    AuctionList = dashboard.AuctionList,
                    Favourites = dashboard.Favourites.Cast<object>().ToList(),  // ? FIXED
                    Transactions = dashboard.Transactions.Cast<object>().ToList(),  // ? FIXED
                    BidList = dashboard.BidList.Select(b => new BidDto
                    {
                        BidId = b.BidId,
                        AuctionId = b.AuctionId,
                        Amount = b.Amount,
                        AuctionTitle = b.Auction?.Listing?.Title ?? "",
                        PlacedAt = b.PlacedAt
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Error fetching buyer dashboard: {ex.Message}");
            }
        }
    }
}
