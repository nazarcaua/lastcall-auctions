using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Models;
using LastCallMotorAuctions.API.Data;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Services
{
    public class ListingService : IListingService
    {
        private readonly ApplicationDbContext _context;

        public ListingService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Create a new listing
        public async Task<ListingResponseDto> CreateListingAsync(CreateListingDto dto, int sellerId)
        {
            var listing = new Listing
            {
                SellerId = sellerId,
                Title = dto.Title,
                Description = dto.Description,
                Year = (short)dto.Year,
                MakeId = dto.MakeId,
                ModelId = dto.ModelId,
                Vin = dto.Vin,
                Mileage = dto.Mileage,
                ConditionGrade = (byte)dto.ConditionGrade,
                LocationId = dto.LocationId,
                StatusId = (byte)1, // Default to Draft
                CreatedAt = DateTime.UtcNow
            };

            _context.Listings.Add(listing);
            await _context.SaveChangesAsync();

            return MapToListingResponseDto(listing);
        }

        // Get a single listing by ID
        public async Task<ListingResponseDto> GetListingByIdAsync(int listingId)
        {
            var listing = await _context.Listings
                .Include(l => l.Make)
                .Include(l => l.Model)
                .Include(l => l.Location)
                .Include(l => l.Status)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.ListingId == listingId);

            if (listing == null)
                throw new KeyNotFoundException("Listing not found");

            return MapToListingResponseDto(listing);
        }

        // Get all listings with optional filtering by seller or status
        public async Task<List<ListingResponseDto>> GetListingsAsync(int? sellerId = null, byte? statusId = null)
        {
            var query = _context.Listings
                .Include(l => l.Make)
                .Include(l => l.Model)
                .Include(l => l.Location)
                .Include(l => l.Status)
                .Include(l => l.Seller)
                .AsQueryable();

            if (sellerId.HasValue)
                query = query.Where(l => l.SellerId == sellerId.Value);

            if (statusId.HasValue)
                query = query.Where(l => l.StatusId == statusId.Value);

            var listings = await query.ToListAsync();
            return listings.Select(MapToListingResponseDto).ToList();
        }

        // Helper to map Listing to ListingResponseDto
        private static ListingResponseDto MapToListingResponseDto(Listing l) => new ListingResponseDto
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
}
