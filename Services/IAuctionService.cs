using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public interface IAuctionService
    {
        Task<AuctionResponseDto> CreateAuctionAsync(CreateAuctionDto createAuctionDto, int sellerId);
        Task<AuctionResponseDto> GetAuctionByIdAsync(int auctionId);
        Task<List<AuctionResponseDto>> GetActiveAuctionAsync();
        Task<BidResponseDto> PlaceBidAsync(PlaceBidDto placeBidDto, int userId);
        Task<bool> IsAuctionActiveAsync(int auctionId);
    }
}
