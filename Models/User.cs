using Microsoft.AspNetCore.Identity;

namespace LastCallMotorAuctions.API.Models
{
    public class User : IdentityUser<int>
    {
        public string FullName { get; set; } = null;
        public byte StatusId { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Navigation
        public UserStatus? Status { get; set; }
    }
}
