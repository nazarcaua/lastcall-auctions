using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Services;

namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionsController : ControllerBase
    {
        private readonly IAuctionService _auctionService;
        private readonly ILogger<AuctionsController> _logger;

        public AuctionsController(IAuctionService auctionService, ILogger<AuctionsController> logger)
        {
            _auctionService = auctionService;
            _logger = logger;
        }

        /// Get all active auctions (for browse list).
        [HttpGet]
        public async Task<IActionResult> GetActiveAuctions()
        {
            var list = await _auctionService.GetActiveAuctionAsync();
            return Ok(list);
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
    }
}
