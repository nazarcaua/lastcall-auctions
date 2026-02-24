using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public interface IAuctionService
    {
        Task<AuctionResponseDto> CreateAuctionAsync(CreateAuctionDto createAuctionDto, int sellerId);
        Task<AuctionBrowseDto?> GetAuctionByIdAsync(int auctionId);
        Task<List<AuctionBrowseDto>> GetActiveAuctionAsync();
        Task<BidResponseDto> PlaceBidAsync(PlaceBidDto placeBidDto, int userId);
        Task<bool> IsAuctionActiveAsync(int auctionId);
        Task<AuctionGroupDetailDto?> GetAuctionGroupByIdAsync(int groupId);
        Task<BuyerDashboardViewModel> GetBuyerDashboardAsync(int buyerId);
    }
}
