using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using LastCallMotorAuctions.API.DTOs;
using LastCallMotorAuctions.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;

namespace LastCallMotorAuctions.API.Controllers;

[ApiController]
[Route("api/[controller]")]

public class ListingsController : Controller
{
    private readonly IListingService _listingService;
    private readonly ILogger<ListingsController> _logger;
    private readonly IWebHostEnvironment _env;

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id)) return null;
        return id;
    }

    public ListingsController(IListingService listingService, ILogger<ListingsController> logger, IWebHostEnvironment env)
    {
        _listingService = listingService;
        _logger = logger;
        _env = env;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateListing([FromBody] CreateListingDto dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var sellerId))
                return Unauthorized();

        try
        {
            var result = await _listingService.CreateListingAsync(dto, sellerId);
            return Ok(result);
        }

        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating listing");

            // Always return detailed exception info
            if (_env.IsDevelopment())
                return StatusCode(500, new
                {
                    message = "An error occurred while creating the listing.",
                    exception = ex.GetType().FullName,
                    details = ex.ToString()
                });
            return StatusCode(500, new { message = "An error occurred while creating the listing." });

        }
    }

    // Get a single listing by ID
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetListing(int id)
    {
        var listing = await _listingService.GetListingByIdAsync(id);
        if (listing == null)
            return NotFound(new { message = "Listing not found." });
        return Ok(listing);
    }

    // Get listings, optionally filtered by sellerId and/or statusId
    [HttpGet]
    public async Task<IActionResult> GetListings([FromQuery] int? sellerId, [FromQuery] byte? statusId)
    {
        var list = await _listingService.GetListingsAsync(sellerId, statusId);
        return Ok(list);
    }

    [HttpPatch("{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateListing(int id, [FromBody] UpdateListingDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _listingService.UpdateListingAsync(id, dto, userId.Value);
            if (result == null) return NotFound(new { message = "Listing not found or access denied." });
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating listing {ListingId}", id);
            // Always include exception details in responses
            if (_env.IsDevelopment())
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the listing.",
                    exception = ex.GetType().FullName,
                    details = ex.ToString()
                });
            return StatusCode(500, new { message = "An error occurred while updating the listing." });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteListing(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var deleted = await _listingService.DeleteListingAsync(id, userId.Value);
        if (!deleted) return NotFound(new { message = "Listing not found or you may only delete your own draft listings." });
        return NoContent();
    }

    [HttpPost("{id:int}/archive")]
    [Authorize]
    public async Task<IActionResult> ArchiveListing(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var archived = await _listingService.ArchiveListingAsync(id, userId.Value);
        if (!archived) return NotFound(new { message = "Listing not found or access denied." });
        return NoContent();
    }

    // =====================
    // Photo upload endpoints
    // =====================

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private const int MaxPhotosPerListing = 10;

    [HttpPost("{id:int}/photos")]
    [Authorize]
    [RequestSizeLimit(52_428_800)] // 50 MB total request limit
    public async Task<IActionResult> UploadPhotos(int id, [FromForm] List<IFormFile> photos)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        // Verify listing exists and belongs to user
        var listing = await _listingService.GetListingByIdAsync(id);
        if (listing == null || listing.SellerId != userId.Value)
            return NotFound(new { message = "Listing not found or access denied." });

        if (photos == null || photos.Count == 0)
            return BadRequest(new { message = "No photos provided." });

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "listings", id.ToString());

        // Count existing photos
        var existingCount = 0;
        if (Directory.Exists(uploadDir))
            existingCount = Directory.GetFiles(uploadDir).Length;

        if (existingCount + photos.Count > MaxPhotosPerListing)
            return BadRequest(new { message = $"Maximum {MaxPhotosPerListing} photos per listing. Currently {existingCount} uploaded." });

        try
        {
            Directory.CreateDirectory(uploadDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create upload directory for listing {ListingId}", id);
            return StatusCode(500, new { message = "Failed to prepare upload directory." });
        }

        var uploaded = new List<string>();

        foreach (var photo in photos)
        {
            if (photo.Length == 0) continue;
            if (photo.Length > MaxFileSize)
                return BadRequest(new { message = $"File '{photo.FileName}' exceeds the 5 MB limit." });

            var ext = Path.GetExtension(photo.FileName);
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { message = $"File type '{ext}' is not allowed. Use .jpg, .jpeg, .png, or .webp." });

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save photo '{FileName}' for listing {ListingId}", photo.FileName, id);
                return StatusCode(500, new { message = $"Failed to save file '{photo.FileName}'. Please try again." });
            }

            uploaded.Add($"/uploads/listings/{id}/{fileName}");
        }

        return Ok(new { message = $"{uploaded.Count} photo(s) uploaded.", urls = uploaded });
    }

    [HttpGet("{id:int}/photos")]
    public IActionResult GetPhotos(int id)
    {
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "listings", id.ToString());

        if (!Directory.Exists(uploadDir))
            return Ok(new { urls = Array.Empty<string>() });

        var urls = Directory.GetFiles(uploadDir)
            .Where(f => AllowedExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => f)
            .Select(f => $"/uploads/listings/{id}/{Path.GetFileName(f)}")
            .ToList();

        return Ok(new { urls });
    }

    [HttpDelete("{id:int}/photos/{fileName}")]
    [Authorize]
    public async Task<IActionResult> DeletePhoto(int id, string fileName)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var listing = await _listingService.GetListingByIdAsync(id);
        if (listing == null || listing.SellerId != userId.Value)
            return NotFound(new { message = "Listing not found or access denied." });

        var filePath = Path.Combine(_env.WebRootPath, "uploads", "listings", id.ToString(), fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "Photo not found." });

        System.IO.File.Delete(filePath);
        return Ok(new { message = "Photo deleted." });
    }
}
