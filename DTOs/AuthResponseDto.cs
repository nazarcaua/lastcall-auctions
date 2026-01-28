namespace LastCallMotorAuctions.API.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public UserResponseDto User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
