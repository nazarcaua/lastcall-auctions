namespace LastCallMotorAuctions.API.DTOs
{
    public class UserResponseDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public byte StatusId { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
