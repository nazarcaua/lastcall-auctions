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
    public class AuctionService : IAuctionService
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

        private List<string> GetPhotoUrlsForListing(int listingId)
        {
            var uploadDir = Path.Combine(_env.WebRootPath ?? "", "uploads", "listings", listingId.ToString());
            if (!Directory.Exists(uploadDir))
                return new List<string>();

            return Directory.GetFiles(uploadDir)
                .Where(f => AllowedPhotoExtensions.Contains(Path.GetExtension(f)))
                .OrderBy(f => f)
                .Select(f => $"/uploads/listings/{listingId}/{Path.GetFileName(f)}")
                .ToList();
        }

        public async Task<BuyerDashboardViewModel> GetBuyerDashboardAsync(int buyerId)
        {
            var buyer = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == buyerId);
            var buyerName = buyer?.FullName ?? "";

            var bids = await _context.Bids
                .AsNoTracking()
                .Where(b => b.BidderId == buyerId)
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
                .Include(a => a.Listing).ThenInclude(l => l!.Location)
                .Include(a => a.Status)
                .FirstOrDefaultAsync(a => a.AuctionId == auctionId);

            if (auction == null) return null;

            // Get highest bid
            var highestBid = await _context.Bids
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.Amount)
                .FirstOrDefaultAsync();

            // Get auction group info if exists
            var groupAuction = await _context.AuctionGroupAuctions
                .Include(aga => aga.AuctionGroup)
                .FirstOrDefaultAsync(aga => aga.AuctionId == auctionId);

            return new AuctionBrowseDto
            {
                AuctionId = auction.AuctionId,
                ListingId = auction.ListingId,
                Title = auction.Listing!.Title,
                Description = auction.Listing.Description,
                Year = auction.Listing.Year,
                MakeName = auction.Listing.Make?.Name ?? "",
                ModelName = auction.Listing.Model?.Name ?? "",
                Vin = auction.Listing.Vin,
                Mileage = auction.Listing.Mileage,
                ConditionGrade = auction.Listing.ConditionGrade,
                City = auction.Listing.Location?.City,
                Region = auction.Listing.Location?.Region,
                Country = auction.Listing.Location?.Country,
                PostalCode = auction.Listing.Location?.PostalCode,
                StartPrice = auction.StartPrice,
                ReservePrice = auction.ReservePrice,
                StartTime = auction.StartTime,
                EndTime = auction.EndTime,
                StatusId = auction.StatusId,
                StatusName = auction.Status?.Name,
                CurrentBid = highestBid?.Amount,
                AuctionGroupId = groupAuction?.AuctionGroupId,
                AuctionGroupTitle = groupAuction?.AuctionGroup?.Title,
                PhotoUrls = GetPhotoUrlsForListing(auction.ListingId)
            };
        }

        public async Task<List<AuctionBrowseDto>> GetActiveAuctionAsync()
        {
            var now = DateTime.UtcNow;

            // Get all auctions that are active (status = 2 for Active, or based on time)
            var auctions = await _context.Auctions
                .AsNoTracking()
                .Include(a => a.Listing).ThenInclude(l => l!.Make)
                .Include(a => a.Listing).ThenInclude(l => l!.Model)
                .Include(a => a.Listing).ThenInclude(l => l!.Location)
                .Include(a => a.Status)
                .Where(a => a.EndTime > now && a.StartTime <= now) // Active based on time
                .OrderBy(a => a.EndTime)
                .ToListAsync();

            // Get all auction IDs
            var auctionIds = auctions.Select(a => a.AuctionId).ToList();

            // Get highest bids for all auctions in one query
            var highestBids = await _context.Bids
                .Where(b => auctionIds.Contains(b.AuctionId))
                .GroupBy(b => b.AuctionId)
                .Select(g => new { AuctionId = g.Key, HighestBid = g.Max(b => b.Amount) })
                .ToDictionaryAsync(x => x.AuctionId, x => x.HighestBid);

            // Get auction group info
            var auctionGroups = await _context.AuctionGroupAuctions
                .Include(aga => aga.AuctionGroup)
                .Where(aga => auctionIds.Contains(aga.AuctionId))
                .ToDictionaryAsync(aga => aga.AuctionId, aga => aga);

            return auctions.Select(a => new AuctionBrowseDto
            {
                AuctionId = a.AuctionId,
                ListingId = a.ListingId,
                Title = a.Listing!.Title,
                Description = a.Listing.Description,
                Year = a.Listing.Year,
                MakeName = a.Listing.Make?.Name ?? "",
                ModelName = a.Listing.Model?.Name ?? "",
                Vin = a.Listing.Vin,
                Mileage = a.Listing.Mileage,
                ConditionGrade = a.Listing.ConditionGrade,
                City = a.Listing.Location?.City,
                Region = a.Listing.Location?.Region,
                Country = a.Listing.Location?.Country,
                PostalCode = a.Listing.Location?.PostalCode,
                StartPrice = a.StartPrice,
                ReservePrice = a.ReservePrice,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                StatusId = a.StatusId,
                StatusName = a.Status?.Name,
                CurrentBid = highestBids.GetValueOrDefault(a.AuctionId),
                AuctionGroupId = auctionGroups.GetValueOrDefault(a.AuctionId)?.AuctionGroupId,
                AuctionGroupTitle = auctionGroups.GetValueOrDefault(a.AuctionId)?.AuctionGroup?.Title,
                PhotoUrls = GetPhotoUrlsForListing(a.ListingId)
            }).ToList();
        }

        public Task<BidResponseDto> PlaceBidAsync(PlaceBidDto placeBidDto, int userId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsAuctionActiveAsync(int auctionId)
        {
            var now = DateTime.UtcNow;
            var auction = await _context.Auctions
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AuctionId == auctionId);

            if (auction == null) return false;

            return auction.StartTime <= now && auction.EndTime > now;
        }

        public async Task<AuctionGroupDetailDto?> GetAuctionGroupByIdAsync(int groupId)
        {
            var group = await _context.AuctionGroups
                .AsNoTracking()
                .Include(g => g.Auctions)
                    .ThenInclude(aga => aga.Auction)
                        .ThenInclude(a => a!.Listing)
                            .ThenInclude(l => l!.Make)
                .Include(g => g.Auctions)
                    .ThenInclude(aga => aga.Auction)
                        .ThenInclude(a => a!.Listing)
                            .ThenInclude(l => l!.Model)
                .Include(g => g.Auctions)
                    .ThenInclude(aga => aga.Auction)
                        .ThenInclude(a => a!.Listing)
                            .ThenInclude(l => l!.Location)
                .Include(g => g.Auctions)
                    .ThenInclude(aga => aga.Auction)
                        .ThenInclude(a => a!.Status)
                .FirstOrDefaultAsync(g => g.AuctionGroupId == groupId);

            if (group == null) return null;

            var auctionIds = group.Auctions.Select(a => a.AuctionId).ToList();

            // Get highest bids
            var highestBids = await _context.Bids
                .Where(b => auctionIds.Contains(b.AuctionId))
                .GroupBy(b => b.AuctionId)
                .Select(g => new { AuctionId = g.Key, HighestBid = g.Max(b => b.Amount) })
                .ToDictionaryAsync(x => x.AuctionId, x => x.HighestBid);

            return new AuctionGroupDetailDto
            {
                AuctionGroupId = group.AuctionGroupId,
                Title = group.Title,
                CreatedAt = group.CreatedAt,
                Auctions = group.Auctions.Select(aga => new AuctionBrowseDto
                {
                    AuctionId = aga.Auction!.AuctionId,
                    ListingId = aga.Auction.ListingId,
                    Title = aga.Auction.Listing!.Title,
                    Description = aga.Auction.Listing.Description,
                    Year = aga.Auction.Listing.Year,
                    MakeName = aga.Auction.Listing.Make?.Name ?? "",
                    ModelName = aga.Auction.Listing.Model?.Name ?? "",
                    Vin = aga.Auction.Listing.Vin,
                    Mileage = aga.Auction.Listing.Mileage,
                    ConditionGrade = aga.Auction.Listing.ConditionGrade,
                    City = aga.Auction.Listing.Location?.City,
                    Region = aga.Auction.Listing.Location?.Region,
                    Country = aga.Auction.Listing.Location?.Country,
                    PostalCode = aga.Auction.Listing.Location?.PostalCode,
                    StartPrice = aga.Auction.StartPrice,
                    ReservePrice = aga.Auction.ReservePrice,
                    StartTime = aga.Auction.StartTime,
                    EndTime = aga.Auction.EndTime,
                    StatusId = aga.Auction.StatusId,
                    StatusName = aga.Auction.Status?.Name,
                    CurrentBid = highestBids.GetValueOrDefault(aga.AuctionId),
                    AuctionGroupId = groupId,
                    AuctionGroupTitle = group.Title,
                    PhotoUrls = GetPhotoUrlsForListing(aga.Auction.ListingId)
                }).ToList()
            };
        }
    }
}
