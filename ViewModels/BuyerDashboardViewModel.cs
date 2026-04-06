using LastCallMotorAuctions.API.Models;

namespace LastCallMotorAuctions.API.ViewModels
{
    public class BuyerDashboardViewModel
    {
        public List<Bid> BidList { get; set; } = new List<Bid>();
        public List<WonAuctionViewModel> WonAuctions { get; set; } = new List<WonAuctionViewModel>();
        public List<Favourite> Favourites { get; set; } = new List<Favourite>();
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = "";
    }

    public class WonAuctionViewModel
    {
        public int AuctionId { get; set; }
        public string VehicleTitle { get; set; } = "";
        public decimal WinningBid { get; set; }
        public DateTime EndTime { get; set; }
        public string? FirstPhotoUrl { get; set; }
    }
}

