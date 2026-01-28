namespace LastCallMotorAuctions.API.Models
{
    public class User : Entity
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public byte StatusId { get; set; }

        // Navigation
        public UserStatus? Status { get; set; }
    }
}
