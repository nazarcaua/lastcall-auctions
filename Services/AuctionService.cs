using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Hubs;
using LastCallMotorAuctions.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Services
{
    public class AuctionService : IAuctionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<BiddingHub> _hubContext;
        public AuctionService(ApplicationDbContext context, IHubContext<BiddingHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public Task<AuctionResponseDto> CreateAuctionAsync(CreateAuctionDto createAuctionDto, int sellerId)
        {
            // TODO: Implement auction creation
            throw new NotImplementedException();
        }

        public Task<AuctionBrowseDto?> GetAuctionByIdAsync(int auctionId)
        {
            // TODO: Implement get auction by ID
            throw new NotImplementedException();
        }

        public async Task<List<AuctionBrowseDto>> GetActiveAuctionAsync()
        {
            const byte activeStatus = 2; // Active

            var auctions = await _context.Auctions
                .AsNoTracking()
                .Where(a => a.StatusId == activeStatus)
                .Include(a => a.Listing)
                    .ThenInclude(l => l!.Make)
                .Include(a => a.Listing)
                    .ThenInclude(l => l!.Model)
                .Include(a => a.Status)
                .OrderBy(a => a.EndTime)
                .ToListAsync();

            if (auctions.Count == 0)
                return new List<AuctionBrowseDto>();

            var auctionIds = auctions.Select(a => a.AuctionId).ToList();
            var maxBids = await _context.Bids
                .AsNoTracking()
                .Where(b => auctionIds.Contains(b.AuctionId))
                .GroupBy(b => b.AuctionId)
                .Select(g => new { AuctionId = g.Key, CurrentBid = g.Max(b => b.Amount) })
                .ToListAsync();

            var bidLookup = maxBids.ToDictionary(x => x.AuctionId, x => (decimal?)x.CurrentBid);

            return auctions.Select(a =>
            {
                var listing = a.Listing!;
                return new AuctionBrowseDto
                {
                    AuctionId = a.AuctionId,
                    ListingId = a.ListingId,
                    Title = listing.Title,
                    Year = listing.Year,
                    MakeName = listing.Make?.Name ?? "",
                    ModelName = listing.Model?.Name ?? "",
                    StartPrice = a.StartPrice,
                    ReservePrice = a.ReservePrice,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    StatusId = a.StatusId,
                    StatusName = a.Status?.Name,
                    CurrentBid = bidLookup.GetValueOrDefault(a.AuctionId)
                };
            }).ToList();
        }

        public Task<BidResponseDto> PlaceBidAsync(PlaceBidDto placeBidDto, int userId)
        {
            // TODO: Implement bid placement
            // When bid is placed, broadcast via SignalR:
            // await _hubContext.Clients.Group($"auction-{placeBidDto.AuctionId}").SendAsync("NewBid", bidData);
            throw new NotImplementedException();
        }

        public Task<bool> IsAuctionActiveAsync(int auctionId)
        {
            // TODO: Implement auction status check
            throw new NotImplementedException();
        }
    }
}
