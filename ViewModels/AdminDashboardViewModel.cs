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
}
