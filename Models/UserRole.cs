using System;

namespace LastCallMotorAuctions.API.Models
{
    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public int? AssignedBy { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
        public Role? Role { get; set; }
    }
}
