using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Services;
using LastCallMotorAuctions.API.DTOs;

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
            // First, try to load as an auction group
            var group = await _auctionService.GetAuctionGroupByIdAsync(id);
            if (group != null && group.Auctions.Count > 0)
            {
                return View(group);
            }

            // Fall back to a single auction and wrap it in a group DTO
            var auction = await _auctionService.GetAuctionByIdAsync(id);
            if (auction == null)
                return NotFound();

            var wrapper = new AuctionGroupDetailDto
            {
                AuctionGroupId = auction.AuctionGroupId ?? auction.AuctionId,
                Title = auction.AuctionGroupTitle ?? auction.Title,
                CreatedAt = auction.StartTime,
                Auctions = new List<AuctionBrowseDto> { auction }
            };

            return View(wrapper);
        }

        /// <summary>
        /// Single auction bidding page with real-time updates.
        /// </summary>
        [HttpGet("[controller]/[action]/{id:int}")]
        public IActionResult Auction(int id)
        {
            // The view loads auction data via JavaScript/API
            ViewData["AuctionId"] = id;
            return View();
        }
    }
}