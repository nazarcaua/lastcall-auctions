namespace LastCallMotorAuctions.API.ViewModels
{
    public class ActiveBidViewModel
    {
        public int ListingId { get; set; }
        public string ListingTitle { get; set; } = "";
        public decimal CurrentPrice { get; set; }
        public decimal YourBid { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class ListingPreviewViewModel
    {
        public int ListingId { get; set; }
        public string Title { get; set; } = "";
        public decimal CurrentPrice { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class BuyerDashboardViewModel
    {
        public int TotalBids { get; set; }
        public int WonAuctions { get; set; }
        public int ActiveBids { get; set; }
        public int Watchlist { get; set; }
        public List<ActiveBidViewModel> ActiveBidsList { get; set; } = [];
        public List<ListingPreviewViewModel> EndingSoon { get; set; } = [];
        public decimal TotalSpent { get; set; }
    }
}
