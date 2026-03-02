using LastCallMotorAuctions.API.Services;
using LastCallMotorAuctions.API.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BuyerDashboardController : ControllerBase
    {
        private readonly IAuctionService _auctionService;

        public BuyerDashboardController(IAuctionService auctionService)
        {
            _auctionService = auctionService;
        }

        [HttpGet("{buyerId:int}")]
        [ProducesResponseType(typeof(BuyerDashboardViewModel), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetDashboard(int buyerId)
        {
            var dashboard = await _auctionService.GetBuyerDashboardAsync(buyerId);

            if (dashboard.BidList.Count == 0 &&
                dashboard.AuctionList.Count == 0 &&
                dashboard.BuyerId == 0)
            {
                return NotFound();
            }

            return Ok(dashboard);
        }
    }
}
