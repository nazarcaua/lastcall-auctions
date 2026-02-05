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

        public ListingService(ApplicationDbContext context, ILogger<ListingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private static ListingResponseDto MapToListingResponseDto(Listing l)
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
                CreatedAt = l.CreatedAt
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

        public async Task<ListingResponseDto> CreateListingAsync(CreateListingDto dto, int sellerId)
        {
            // Validate Model belongs to Make
            var model = await _context.VehicleModels.FindAsync(dto.ModelId);
            if (model == null)
                throw new ArgumentException("Invalid ModelId.");
            if (model.MakeId != dto.MakeId) 
                throw new ArgumentException("Model does not belong to the selected Make.");

            // Find or create Locatoin
            var region = string.IsNullOrWhiteSpace(dto.Region) ? null : dto.Region.Trim();
            var postalCode = string.IsNullOrWhiteSpace(dto.PostalCode) ? null : dto.PostalCode.Trim();

            var location = await _context.Locations
                .FirstOrDefaultAsync(l =>
                l.City == dto.City.Trim() &&
                l.Country == dto.Country.Trim() &&
                l.Region == region &&
                l.PostalCode == postalCode);

            if (location == null)
            {
                location = new Location
                {
                    City = dto.City.Trim(),
                    Region = region,
                    Country = dto.Country.Trim(),
                    PostalCode = postalCode
                };
                _context.Locations.Add(location);
                await _context.SaveChangesAsync();
            }

            // Create and Save Listing
            var listing = new Listing
            {
                SellerId = sellerId,
                Title = dto.Title,
                Description = dto.Description,
                Year = dto.Year,
                MakeId = dto.MakeId,
                ModelId = dto.ModelId,
                Vin = dto.Vin,
                Mileage = dto.Mileage,
                ConditionGrade = dto.ConditionGrade,
                LocationId = location.LocationId,
                StatusId = 1 
            };

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            return MapToListingResponseDto(listing);
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
