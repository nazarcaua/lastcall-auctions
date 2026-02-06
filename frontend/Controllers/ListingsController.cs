//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Http;
//using System.Security.Claims;
//using LastCallMotorAuctions.API.DTOs;
//using LastCallMotorAuctions.API.Services;
//using System.IO;

//namespace LastCallMotorAuctions.API.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class ListingsController(
//        IListingService listingService,
//        ILogger<ListingsController> logger,
//        IWebHostEnvironment env) : ControllerBase
//    {
//        private readonly IListingService ListingService = listingService;
//        private readonly ILogger<ListingsController> Logger = logger;
//        private readonly IWebHostEnvironment Environment = env;

//        // FIXED: Modern C# 12 collection expression
//        private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif"];

//        [HttpPost]
//        [Authorize]
//        [Consumes("multipart/form-data")]
//        public async Task<IActionResult> CreateListing([FromForm] CreateListingDto dto, IFormFile? imageFile)
//        {
//            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//            if (!int.TryParse(userIdClaim, out var sellerId))
//                return Unauthorized();

//            try
//            {
//                if (imageFile is { Length: > 0 })
//                {
//                    var imagePath = await SaveImageAsync(imageFile);
//                    dto.ImagePath = imagePath;
//                }

//                var result = await ListingService.CreateListingAsync(dto, sellerId);
//                return CreatedAtAction(nameof(GetListing), new { id = result.ListingId }, result);
//            }
//            catch (ArgumentException ex)
//            {
//                return BadRequest(new { message = ex.Message });
//            }
//            catch (Exception ex)
//            {
//                Logger.LogError(ex, "Error creating listing");
//                return StatusCode(500);
//            }
//        }

//        [HttpGet("{id}")]
//        [Authorize]
//        public async Task<IActionResult> GetListing(int id)
//        {
//            var result = await ListingService.GetListingByIdAsync(id);
//            return result == null ? NotFound() : Ok(result);
//        }

//        private async Task<string> SaveImageAsync(IFormFile imageFile)
//        {
//            var uploadsFolder = Path.Combine(Environment.WebRootPath, "images", "listings");
//            Directory.CreateDirectory(uploadsFolder);

//            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
//            if (!AllowedExtensions.Contains(ext))
//                throw new ArgumentException("Invalid image format");

//            var filename = $"{Guid.NewGuid():N}{ext}";
//            var filepath = Path.Combine(uploadsFolder, filename);

//            await using var stream = new FileStream(filepath, FileMode.Create);
//            await imageFile.CopyToAsync(stream);

//            return $"/images/listings/{filename}";
//        }
//    }
//}
