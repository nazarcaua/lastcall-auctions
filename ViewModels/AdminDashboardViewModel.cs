namespace LastCallMotorAuctions.API.ViewModels
{
    public class AdminDashboardViewModel
    {
        public List<AdminUserViewModel> Users { get; set; } = new();

        // Stats
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int SuspendedUsers { get; set; }
        public int BuyerCount { get; set; }
        public int SellerCount { get; set; }
        public int AdminCount { get; set; }
        public int TotalAuctions { get; set; }
        public int ActiveAuctions { get; set; }
        public int TotalListings { get; set; }

        // All auctions for admin management
        public List<AdminAuctionViewModel> Auctions { get; set; } = new();
    }

    public class AdminUserViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public byte StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public DateTime? LockoutEnd { get; set; }
    }

    public class AdminAuctionViewModel
    {
        public int AuctionId { get; set; }
        public int? AuctionGroupId { get; set; }
        public string? AuctionGroupTitle { get; set; }
        public string Title { get; set; } = string.Empty;
        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string SellerEmail { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public byte StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public decimal StartPrice { get; set; }
        public decimal? CurrentBid { get; set; }
        public int BidCount { get; set; }
        public bool HasStarted { get; set; }
        public bool HasEnded { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
