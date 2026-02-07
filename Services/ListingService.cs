using System.Runtime.InteropServices;
using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Services
{
    public class ListingService : IListingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListingService> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> AllowedPhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public ListingService(ApplicationDbContext context, ILogger<ListingService> logger, IWebHostEnvironment env)
        {
            _context = context;
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

        private ListingResponseDto MapToListingResponseDto(Listing l)
        {
            return new ListingResponseDto
            {
                ListingId = l.ListingId,
                SellerId = l.SellerId,
                Title = l.Title,
                Description = l.Description,
                Year = l.Year,
                MakeId = l.MakeId,
                ModelId = l.ModelId,
                Vin = l.Vin,
                Mileage = l.Mileage,
                ConditionGrade = l.ConditionGrade,
                LocationId = l.LocationId,
                StatusId = l.StatusId,
                CreatedAt = l.CreatedAt,

                City = l.Location?.City,
                Region = l.Location?.Region,
                Country = l.Location?.Country,
                PostalCode = l.Location?.PostalCode,

                PhotoUrls = GetPhotoUrlsForListing(l.ListingId)
            };
        }

        public async Task<ListingResponseDto?> GetListingByIdAsync(int listingId)
        {
            var listing = await _context.Listings
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ListingId == listingId);

            if (listing == null)
                return null;

            return MapToListingResponseDto(listing);
        }

        public async Task<List<ListingResponseDto>> GetListingsAsync(int? sellerId = null, byte? statusId = null)
        {
            var query = _context.Listings.AsNoTracking();

            if (sellerId.HasValue)
                query = query.Where(l => l.SellerId == sellerId.Value);

            if (statusId.HasValue)
                query = query.Where(l => l.StatusId == statusId.Value);

            var list = await query
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return list.Select(MapToListingResponseDto).ToList();
        }

        public async Task<List<ListingResponseDto>> CreateListingAsync(CreateListingDto dto, int sellerId)
        {
            if (dto.Vehicles == null || dto.Vehicles.Count == 0)
                throw new ArgumentException("At least one vehicle is required.");

            // If group title provided, create group
            AuctionGroup? group = null;
            if (!string.IsNullOrWhiteSpace(dto.AuctionGroupTitle))
            {
                group = new AuctionGroup { Title = dto.AuctionGroupTitle.Trim() };
                _context.AuctionGroups.Add(group);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                {
                    // If the AuctionGroups table doesn't exist (migrations not applied), log and continue without grouping
                    _logger.LogWarning(ex, "Failed to create AuctionGroup (table may not exist). Continuing without group.");
                    // Detach the entity to avoid tracking issues
                    _context.Entry(group).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    group = null;
                }
            }

            var createdListings = new List<ListingResponseDto>();

            // If a group EndTime is provided, compute group start and validate once
            DateTime? groupStartUtc = null;
            DateTime? groupEndUtc = null;
            if (dto.EndTime.HasValue)
            {
                groupStartUtc = DateTime.UtcNow;
                groupEndUtc = dto.EndTime.Value.ToUniversalTime();
                if (groupEndUtc <= groupStartUtc.Value.AddMinutes(1))
                    throw new ArgumentException("Auction end time must be at least 1 minute in the future.");

                _logger.LogInformation("Creating auction group '{Title}' with StartUtc={StartUtc:o} EndUtc={EndUtc:o}", dto.AuctionGroupTitle ?? "(none)", groupStartUtc.Value, groupEndUtc.Value);
            }

            try
            {
                foreach (var vehicle in dto.Vehicles)
                {
                    // Validate model
                    var model = await _context.VehicleModels.FindAsync(vehicle.ModelId);
                    if (model == null) throw new ArgumentException("Invalid ModelId.");
                    if (model.MakeId != vehicle.MakeId) throw new ArgumentException("Model does not belong to the selected Make.");

                    // Use placeholder location for now (seller must provide location?) Keep simple: use a default location or require inputs later
                    var defaultLocation = await _context.Locations.FirstOrDefaultAsync();
                    if (defaultLocation == null)
                    {
                        defaultLocation = new Location { City = "Unknown", Country = "Unknown" };
                        _context.Locations.Add(defaultLocation);
                        await _context.SaveChangesAsync();
                    }

                    var listing = new Listing
                    {
                        SellerId = sellerId,
                        Title = vehicle.Title,
                        Description = vehicle.Description,
                        Year = vehicle.Year,
                        MakeId = vehicle.MakeId,
                        ModelId = vehicle.ModelId,
                        Vin = vehicle.Vin,
                        Mileage = vehicle.Mileage,
                        ConditionGrade = vehicle.ConditionGrade,
                        LocationId = defaultLocation.LocationId,
                        StatusId = 1
                    };

                    _context.Listings.Add(listing);
                    await _context.SaveChangesAsync();

                    // Create auction for each listing if group EndTime provided
                    if (groupEndUtc.HasValue && groupStartUtc.HasValue)
                    {
                        var startTime = groupStartUtc.Value;
                        var endTimeUtc = groupEndUtc.Value;

                        var auction = new Auction
                        {
                            ListingId = listing.ListingId,
                            StartPrice = vehicle.StartPrice,
                            ReservePrice = vehicle.ReservePrice,
                            StartTime = startTime,
                            EndTime = endTimeUtc,
                            // set status depending on whether endTime is in the future relative to the group start
                            StatusId = (byte)(endTimeUtc > startTime ? 2 : 3)
                        };
                        _context.Auctions.Add(auction);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Created Auction {AuctionId} for Listing {ListingId}: Start={Start:o} End={End:o} Status={Status}", auction.AuctionId, listing.ListingId, auction.StartTime, auction.EndTime, auction.StatusId);
                        if (auction.StatusId == 3)
                        {
                            _logger.LogWarning("Auction {AuctionId} was created with Status=Ended because EndTime <= StartTime; Start={Start:o} End={End:o}", auction.AuctionId, auction.StartTime, auction.EndTime);
                        }

                        if (group != null)
                        {
                            try
                            {
                                _context.AuctionGroupAuctions.Add(new AuctionGroupAuction { AuctionGroupId = group.AuctionGroupId, AuctionId = auction.AuctionId });
                                await _context.SaveChangesAsync();
                            }
                            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                            {
                                // If linking failed because the table doesn't exist, log and continue
                                _logger.LogWarning(ex, "Failed to link Auction to AuctionGroup (table may not exist). Continuing without group link.");
                            }
                        }

                        // set listing status to match auction status
                        listing.StatusId = (byte)(endTimeUtc > startTime ? 2 : 3);
                        await _context.SaveChangesAsync();
                    }

                    createdListings.Add(MapToListingResponseDto(listing));
                }

                return createdListings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating listings/auctions");
                throw;
            }
        }

        public async Task<ListingResponseDto?> UpdateListingAsync(int lisitngId, UpdateListingDto dto, int userId)
        {
            var listing = await _context.Listings
                .Include(l => l.Location)
                .FirstOrDefaultAsync(l => l.ListingId == lisitngId);

            if (listing == null)
                return null;

            if (listing.SellerId != userId)
                throw new UnauthorizedAccessException("You can only update your own listings.");

            // Validate Make/Model if both are provided
            if (dto.MakeId.HasValue && dto.ModelId.HasValue)
            {
                var model = await _context.VehicleModels.FindAsync(dto.ModelId.Value);
                if (model == null)
                    throw new ArgumentException("Invalid ModelId.");
                if (model.MakeId != dto.MakeId.Value)
                    throw new ArgumentException("Model does not belong to the selected Make.");
                listing.MakeId = dto.MakeId.Value;
                listing.ModelId = dto.ModelId.Value;
            }
            else if (dto.MakeId.HasValue)
            {
                listing.MakeId = dto.MakeId.Value;
            }
            else if (dto.ModelId.HasValue)
            {
                listing.ModelId = dto.ModelId.Value;
            }

            if (dto.Title != null)
                listing.Title = dto.Title.Trim();
            if (dto.Description != null)
                listing.Description = dto.Description.Trim();
            if (dto.Year.HasValue)
                listing.Year = dto.Year.Value;
            if (dto.Vin != null)
                listing.Vin = string.IsNullOrWhiteSpace(dto.Vin) ? null : dto.Vin.Trim();
            if (dto.Mileage.HasValue)
                listing.Mileage = dto.Mileage.Value;
            if (dto.ConditionGrade.HasValue)
                listing.ConditionGrade = dto.ConditionGrade.Value;
            if (dto.StatusId.HasValue)
                listing.StatusId = dto.StatusId.Value;

            // Location: Find / Create if any address field is provided
            if (dto.City != null || dto.Country != null || dto.Region != null || dto.PostalCode != null)
            {
                var city = dto.City?.Trim() ?? listing.Location?.City ?? "";
                var country = dto.Country?.Trim() ?? listing.Location?.Country ?? "";
                if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country))
                    throw new ArgumentException("City and Country are requied when update location.");

                var region = string.IsNullOrWhiteSpace(dto.Region) ? listing.Location?.Region : dto.Region.Trim();
                var postalCode = string.IsNullOrWhiteSpace(dto.PostalCode) ? listing.Location?.PostalCode : dto.PostalCode.Trim();

                var location = await _context.Locations
                    .FirstOrDefaultAsync(l =>
                    l.City == city &&
                    l.Country == country &&
                    l.Region == region &&
                    l.PostalCode == postalCode);

                if (location == null)
                {
                    location = new Location
                    {
                        City = city,
                        Country = country,
                        Region = region,
                        PostalCode = postalCode
                    };
                    _context.Locations.Add(location);
                    await _context.SaveChangesAsync();
                }
                listing.LocationId = location.LocationId;
            }
            await _context.SaveChangesAsync();
            return MapToListingResponseDto(listing);
        }

        public async Task<bool> DeleteListingAsync(int listingId, int userId)
        {
            var listing = await _context.Listings.FindAsync(listingId);
            if (listing == null || listing.SellerId != userId)
                return false;

            // Only allow delete if its a draft (StatusId = 1)
            if (listing.StatusId != 1)
                return false;

            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ArchiveListingAsync(int listingId, int userId)
        {
            var listing = await _context.Listings.FindAsync(listingId);
            if (listing == null || listing.SellerId != userId)
                return false;

            listing.StatusId = 3; // Archived
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
