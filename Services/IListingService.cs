using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public interface IListingService
    {
        Task<ListingResponseDto> CreateListingAsync(CreateListingDto dto, int sellerId);
        Task<ListingResponseDto> GetListingByIdAsync(int listingId);
        Task<List<ListingResponseDto>> GetListingsAsync(int? sellerId = null, byte? statusId = null);
        Task<ListingResponseDto?> UpdateListingAsync(int listingId, UpdateListingDto dto, int userId);
        Task<bool> DeleteListingAsync(int listingId, int userId);
        Task<bool> ArchiveListingAsync(int listingId, int userId);
    }
}
