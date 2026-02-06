using LastCallMotorAuctions.API.Models;

namespace LastCallMotorAuctions.API.ViewModels
{
    public class ListingAuctionViewModel
    {
        public Listing Listing { get; set; } = null!;
        public Auction? Auction { get; set; } 
    }
}
