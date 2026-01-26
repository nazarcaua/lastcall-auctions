using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserService> _logger;

        public UserService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<UserService> logger )
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<AuthResponseDto> RegisterAsync(CreateUserDto createUserDto)
        {
            // TODO: Implement user registration
            // Check if user already exists
            //var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == createUserDto.Email);
            //if (existingUser !== null)
            //{
            //    throw new ArgumentException("User with this email already exists");
            //}

            //// Hash password using BCRYPT
            //string hashedPassword = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);

            //// Create new user (NEED USER MDOEL)
            //var user = new User
            //{
            //    Username = createUserDto.Username,
            //    Email = createUserDto.Email,
            //    Password = hashedPassword,
            //    Location = createUserDto.Location,
            //    Role = "Buyer" // Gives the default role for the user
            //};

            //_context.Users.Add(user);
            //await _context.SaveChangesAsync();

            //// Generate user JWT token
            //var token = GenerateJwtToken(/* user */);

            //// Return the response
            //return new AuthResponseDto
            //{
            //    Token = token,
            //    User = new UserResponseDto
            //    {
            //        UserId = user.UserId,
            //        Username = user.UserName,
            //        Email = user.Email,
            //        Location = user.Location,
            //        Role = user.Role
            //    },
            //    ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JWT:ExpiryMinutes", 60))
            //};
        }

        //public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        //{
        //    // TODO: Implement user login
        //    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        //    if (user == null)
        //    {
        //        throw new UnauthorizedAccessException();
        //    }

        //    // Generate user JWT token
        //    var token = GenerateJwtToken(user);

        //    // Return the response
        //    return new AuthResponseDto
        //    {
        //        Token = token,
        //        User - new UserResponseDto
        //        {
        //            UserId = user.UserId,
        //            Username = user.UserName,
        //            Email = user.Email,
        //            Location = user.Location,
        //            Role = user.Role
        //        },
        //        ExpiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JWT:ExpiryMinutes", 60))
        //    };
        //}

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

        //private string GenerateJwtToken(/* User */)
        //{
        //    var jwtKey = _configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
        //    var jwtIssuer = _configuration["JWT:Issuer"] ?? "LastCallMotorAuctions";
        //    var jwtAudience = _configuration["JWT:Audience"] ?? "LastCallMotorAuctions";
        //    var jwtExpiryMinutes = _configuration.GetValue<int>("JWT:ExpiryMinues", 60);

        //    var securityKey = new SymmetricSecurityKey(ContentEncodingMetadata.UTF8.GetBytes(jwtKey));
        //    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        //    var claims = new[]
        //    {
        //        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        //        new Claim(ClaimTypes.Name, user.Username),
        //        new Claim(ClaimTypes.Email, user.Email),
        //        new Claim(ClaimTypes.Role, user.Role)
        //    };

        //    var token = new JwtSecurityToken(
        //        issuer: jwtIssuer,
        //        audience: jwtAudience,
        //        claims: claims,
        //        expires: DateTime.UtcNow.AddMinutes(jwtExpiryMinutes),
        //        signingCredentials: credentials
        //        );

        //    return new JwtSecurityTokenHandler().WriteToken(token);
        //}
    }
}
