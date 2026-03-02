using LastCallMotorAuctions.API.Models;
using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.ViewModels
{
    public class BuyerDashboardViewModel
    {
        public List<Bid> BidList { get; set; } = new();
        public List<AuctionBrowseDto> AuctionList { get; set; } = new();
        public List<object> Favourites { get; set; } = new();
        public List<object> Transactions { get; set; } = new();
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
    }
}
