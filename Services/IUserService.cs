using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public interface IUserService
    {
        Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<UserResponseDto?> GetUserByIdAsync(int userId);
        Task<bool> RequestSellerAccountAsync(int userId);
    }
}
