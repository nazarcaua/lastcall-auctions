using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Hubs;
using LastCallMotorAuctions.API.Models;
using LastCallMotorAuctions.API.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LastCallMotorAuctions.API.Services
{
    public class AuctionService : IAuctionService  // ← Uses your EXISTING interface
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<BiddingHub> _hubContext;
        private readonly ILogger<AuctionService> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> AllowedPhotoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public AuctionService(ApplicationDbContext context, IHubContext<BiddingHub> hubContext,
            ILogger<AuctionService> logger, IWebHostEnvironment env)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
            _env = env;
        }

        public async Task<BuyerDashboardViewModel> GetBuyerDashboardAsync(int buyerId)
        {
            var buyer = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == buyerId);
            var buyerName = buyer?.FullName ?? "";

            var bids = await _context.Bids
                .AsNoTracking()
                .Where(b => b.BidderId == buyerId)  // or b.BidderId if that's your property
                .Include(b => b.Auction)
                .ToListAsync();

            var auctionIds = bids.Select(b => b.AuctionId).Distinct().ToList();
            var auctions = new List<AuctionBrowseDto>();

            foreach (var aid in auctionIds)
            {
                var auctionDto = await GetAuctionByIdAsync(aid);
                if (auctionDto != null) auctions.Add(auctionDto);
            }

            return new BuyerDashboardViewModel
            {
                BuyerId = buyerId,
                BuyerName = buyerName,
                BidList = bids,
                AuctionList = auctions,
                Favourites = new List<Favourite>(),
                Transactions = new List<Transaction>(),
                SellerRatings = new List<SellerRatingDto>()
            };
        }

        // Keep all your other methods exactly as they were
        public Task<AuctionResponseDto> CreateAuctionAsync(CreateAuctionDto createAuctionDto, int sellerId)
        {
            throw new NotImplementedException();
        }

        public async Task<AuctionBrowseDto?> GetAuctionByIdAsync(int auctionId)
        {
            var auction = await _context.Auctions
                .AsNoTracking()
                .Include(a => a.Listing).ThenInclude(l => l!.Make)
                .Include(a => a.Listing).ThenInclude(l => l!.Model)
                .Include(a => a.Status)
                .FirstOrDefaultAsync(a => a.AuctionId == auctionId);

            if (auction == null) return null;

            return new AuctionBrowseDto
            {
                AuctionId = auction.AuctionId,
                ListingId = auction.ListingId,
                Title = auction.Listing!.Title
                // ... rest of properties
            };
        }

        public async Task<List<AuctionBrowseDto>> GetActiveAuctionAsync()
        {
            return new List<AuctionBrowseDto>();
        }

        public Task<BidResponseDto> PlaceBidAsync(PlaceBidDto placeBidDto, int userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsAuctionActiveAsync(int auctionId)
        {
            throw new NotImplementedException();
        }

        public async Task<AuctionGroupDetailDto?> GetAuctionGroupByIdAsync(int groupId)
        {
            return null;
        }
    }
}
