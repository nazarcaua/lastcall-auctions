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
        private readonly ILogger<AuctionService> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public AuctionService(ApplicationDbContext context, IHubContext<BiddingHub> hubContext, ILogger<AuctionService> logger, IWebHostEnvironment env)
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

        public Task<AuctionResponseDto> CreateAuctionAsync(CreateAuctionDto createAuctionDto, int sellerId)
        {
            // TODO: Implement auction creation
            throw new NotImplementedException();
        }

        public async Task<AuctionBrowseDto?> GetAuctionByIdAsync(int auctionId)
        {
            var auction = await _context.Auctions
                .AsNoTracking()
                .Include(a => a.Listing)
                    .ThenInclude(l => l!.Make)
                .Include(a => a.Listing)
                    .ThenInclude(l => l!.Model)
                .Include(a => a.Status)
                .FirstOrDefaultAsync(a => a.AuctionId == auctionId);

            if (auction == null)
                return null;

            var currentBid = await _context.Bids
                .AsNoTracking()
                .Where(b => b.AuctionId == auctionId)
                .MaxAsync(b => (decimal?)b.Amount);

            var listing = auction.Listing!;

            // Find group if exists (table may not exist if migrations not applied)
            int? groupId = null; string? groupTitle = null;
            try
            {
                var groupJoin = await _context.AuctionGroupAuctions.AsNoTracking().FirstOrDefaultAsync(aga => aga.AuctionId == auctionId);
                if (groupJoin != null)
                {
                    groupId = groupJoin.AuctionGroupId;
                    groupTitle = (await _context.AuctionGroups.AsNoTracking().FirstOrDefaultAsync(g => g.AuctionGroupId == groupId))?.Title;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auction group tables not available when fetching auction {AuctionId}", auctionId);
            }

            return new AuctionBrowseDto
            {
                AuctionId = auction.AuctionId,
                ListingId = auction.ListingId,
                Title = listing.Title,
                Year = listing.Year,
                MakeName = listing.Make?.Name ?? "",
                ModelName = listing.Model?.Name ?? "",
                StartPrice = auction.StartPrice,
                ReservePrice = auction.ReservePrice,
                StartTime = auction.StartTime,
                EndTime = auction.EndTime,
                StatusId = auction.StatusId,
                StatusName = auction.Status?.Name,
                CurrentBid = currentBid,
                AuctionGroupId = groupId,
                AuctionGroupTitle = groupTitle,
                PhotoUrls = GetPhotoUrlsForListing(auction.ListingId)
            };
        }

        public async Task<List<AuctionBrowseDto>> GetActiveAuctionAsync()
        {
            const byte activeStatus = 2; // Active
            var now = DateTime.UtcNow;

            var auctions = await _context.Auctions
                .AsNoTracking()
                .Where(a => a.StatusId == activeStatus && a.EndTime > now)
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

            // load group joins for all auctions — wrapped in try/catch because the AuctionGroup tables may not exist yet
            List<AuctionGroupAuction> groupJoins = new();
            List<AuctionGroup> groups = new();
            Dictionary<int, int> groupLookup = new();
            try
            {
                groupJoins = await _context.AuctionGroupAuctions.AsNoTracking()
                    .Where(aga => auctionIds.Contains(aga.AuctionId))
                    .ToListAsync();
                var groupIds = groupJoins.Select(g => g.AuctionGroupId).Distinct().ToList();
                if (groupIds.Count > 0)
                {
                    groups = await _context.AuctionGroups.AsNoTracking().Where(g => groupIds.Contains(g.AuctionGroupId)).ToListAsync();
                    groupLookup = groupJoins.ToDictionary(g => g.AuctionId, g => g.AuctionGroupId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AuctionGroup tables not available when fetching active auctions; continuing without grouping.");
            }

            return auctions.Select(a =>
            {
                var listing = a.Listing!;
                int? gid = groupLookup.ContainsKey(a.AuctionId) ? groupLookup[a.AuctionId] : (int?)null;
                var gtitle = gid.HasValue ? groups.FirstOrDefault(g => g.AuctionGroupId == gid.Value)?.Title : null;

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
                    CurrentBid = bidLookup.GetValueOrDefault(a.AuctionId),
                    AuctionGroupId = gid,
                    AuctionGroupTitle = gtitle,
                    PhotoUrls = GetPhotoUrlsForListing(a.ListingId)
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

        public async Task<AuctionGroupDetailDto?> GetAuctionGroupByIdAsync(int groupId)
        {
            var group = await _context.AuctionGroups.AsNoTracking().FirstOrDefaultAsync(g => g.AuctionGroupId == groupId);
            if (group == null) return null;

            var joins = await _context.AuctionGroupAuctions.AsNoTracking().Where(aga => aga.AuctionGroupId == groupId).ToListAsync();
            var auctionIds = joins.Select(j => j.AuctionId).ToList();

            var auctions = new List<AuctionBrowseDto>();
            foreach (var aid in auctionIds)
            {
                var a = await GetAuctionByIdAsync(aid);
                if (a != null) auctions.Add(a);
            }

            return new AuctionGroupDetailDto { AuctionGroupId = group.AuctionGroupId, Title = group.Title, CreatedAt = group.CreatedAt, Auctions = auctions };
        }
    }
}
