using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace LastCallMotorAuctions.API.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto)
        {
            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("Email is already registered.");
            }

            // Create Identity user object
            var user = new User
            {
                UserName = createUserDto.Email,
                Email = createUserDto.Email,
                FullName = createUserDto.FullName,
                StatusId = 1
            };

            // Identity hashing password and creating the user
            var result = await _userManager.CreateAsync(user, createUserDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ArgumentException($"Registration failed: {errors}");
            }

            // Assign Buyer role (role is seeded on app startup via RoleSeeder)
            const string buyerRoleName = "Buyer";
            var addToRoleResult = await _userManager.AddToRoleAsync(user, buyerRoleName);
            if (!addToRoleResult.Succeeded)
            {
                var errors = string.Join(", ", addToRoleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign Buyer role to user {UserId}: {Errors}", user.Id, errors);
                throw new InvalidOperationException("Failed to assign Buyer role to user.");
            }

            // Generate JWT Token
            var token = await GenerateJwtToken(user);
            var expiryMinutes = _configuration.GetValue<int>("JWT:ExpiryMinutes", 60);

            return new AuthResponseDto
            {
                Token = token,
                User = new UserResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    StatusId = user.StatusId,
                    Roles = new List<string> { buyerRoleName }
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // Check if user is active
            if (user.StatusId != 1)
            {
                throw new UnauthorizedAccessException("Account is not active.");
            }

            // Verify password using Identity
            var signInResult = await _signInManager.CheckPasswordSignInAsync(
                user,
                loginDto.Password,
                lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                if (signInResult.IsLockedOut)
                {
                    throw new UnauthorizedAccessException("Account is locked out. Please try again later.");
                }

                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // Sign in with cookie for MVC views
            await _signInManager.SignInAsync(user, isPersistent: true);

            // Generate JWT Token
            var token = await GenerateJwtToken(user);
            var expiryMinutes = _configuration.GetValue<int>("JWT:ExpiryMinutes", 60);
            var roles = await _userManager.GetRolesAsync(user);

            return new AuthResponseDto
            {
                Token = token,
                User = new UserResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    StatusId = user.StatusId,
                    Roles = roles.ToList()
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes)
            };
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserResponseDto
            {
                UserId = userId,
                Email = user.Email,
                FullName = user.FullName,
                StatusId = user.StatusId,
                Roles = roles.ToList()
            };
        }

        public Task<bool> RequestSellerAccountAsync(int userId)
        {
            // TODO: Implement seller account request flow (e.g., mark a flag, send notification to admin, etc.)
            throw new NotImplementedException();
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["JWT:Key"]
                ?? throw new InvalidOperationException("JWT Key is not configured");
            var jwtIssuer = _configuration["JWT:Issuer"] ?? "LastCallMotorAuctions";
            var jwtAudience = _configuration["JWT:Audience"] ?? "LastCallMotorAuctions";
            var jwtExpiryMinutes = _configuration.GetValue<int>("JWT:ExpiryMinutes", 60);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Get user roles from identity
            var roles = await _userManager.GetRolesAsync(user);

            // Build claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.FullName)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
               issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwtExpiryMinutes),
                signingCredentials: credentials
             );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}