using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public class UserService : IUserService
    {
        public Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto)
        {
            // TODO: Implement user registration
            throw new NotImplementedException();
        }

        public Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // TODO: Implement user login
            throw new NotImplementedException();
        }

        public Task<UserResponseDto?> GetUserByIdAsync(int userId)
        {
            // TODO: Implement get user by ID
            throw new NotImplementedException();
        }

        public Task<bool> RequestSellerAccountAsync(int userId)
        {
            // TODO: Implement seller account request
            throw new NotImplementedException();
        }
    }
}
