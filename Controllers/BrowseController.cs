using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Services;

namespace LastCallMotorAuctions.API.Controllers
{
    /// Serves the browse auctions page (HTML). API remains under /api/auctions.
    public class BrowseController : Controller
    {
        private readonly IAuctionService _auctionService;

        public BrowseController(IAuctionService auctionService)
        {
            _auctionService = auctionService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var list = await _auctionService.GetActiveAuctionAsync();
            return View(list);
        }
        
        [HttpGet("[controller]/[action]/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var auction = await _auctionService.GetAuctionByIdAsync(id);
            if (auction == null)
                return NotFound();
            return View(auction);
        }
    }
}