using LastCallMotorAuctions.API.Models;

namespace LastCallMotorAuctions.API.ViewModels
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public byte StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public bool IsOwnProfile { get; set; }

        // Seller auction data
        public bool IsSeller { get; set; }
        public bool IsBuyer { get; set; }
        public List<AuctionGroupViewModel> ActiveAuctionGroups { get; set; } = new();
        public List<Auction> RecentlyEndedAuctions { get; set; } = new();
    }

    public class EditProfileViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public byte StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
