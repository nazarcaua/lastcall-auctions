using LastCallMotorAuctions.API.Models;

namespace LastCallMotorAuctions.API.ViewModels
{
    public class SellerDashboardViewModel
    {
        public List<Auction> Auctions { get; set; } = new();
        
        // Stats
        public int TotalListings { get; set; }
        public int ActiveListings { get; set; }
        public int EndedListings { get; set; }
        public int TotalBids { get; set; }
        public decimal TotalRevenue { get; set; }
        public int NeedsAttentionCount { get; set; }
        
        // Current seller info
        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
    }
}
