using LastCallMotorAuctions.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/admin/vehicles")]
    public class VehicleAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VehicleAdminController> _logger;

        public VehicleAdminController(ApplicationDbContext context, ILogger<VehicleAdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Clears all vehicle data and reseeds from NHTSA.
        /// WARNING: This will delete all existing vehicle data!
        /// </summary>
        [HttpPost("reseed-from-nhtsa")]
        // [Authorize(Policy = "AdminOnly")] // Uncomment in production!
        public async Task<IActionResult> ReseedFromNhtsa()
        {
            try
            {
                _logger.LogWarning("Starting vehicle data reseed from NHTSA...");

                // Delete existing data in correct order (respecting foreign keys)
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM VehicleYearMakeModels");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM VehicleYearMakes");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM VehicleModels");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM VehicleMakes");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM VehicleYears");

                _logger.LogInformation("Cleared existing vehicle data");

                // Reseed from NHTSA
                await NhtsaVehicleDataSeeder.SeedFromNhtsaAsync(_context, _logger);

                var stats = new
                {
                    Years = await _context.VehicleYears.CountAsync(),
                    Makes = await _context.VehicleMakes.CountAsync(),
                    Models = await _context.VehicleModels.CountAsync(),
                    YearMakes = await _context.VehicleYearMakes.CountAsync(),
                    YearMakeModels = await _context.VehicleYearMakeModels.CountAsync()
                };

                return Ok(new
                {
                    message = "Vehicle data reseeded successfully from NHTSA",
                    stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reseeding vehicle data");
                return StatusCode(500, new { message = "Error reseeding vehicle data", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets current vehicle data statistics.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = new
            {
                Years = await _context.VehicleYears.CountAsync(),
                Makes = await _context.VehicleMakes.CountAsync(),
                Models = await _context.VehicleModels.CountAsync(),
                YearMakes = await _context.VehicleYearMakes.CountAsync(),
                YearMakeModels = await _context.VehicleYearMakeModels.CountAsync(),
                SampleData = new
                {
                    YearRange = new
                    {
                        Min = await _context.VehicleYears.MinAsync(y => (int?)y.Year) ?? 0,
                        Max = await _context.VehicleYears.MaxAsync(y => (int?)y.Year) ?? 0
                    },
                    TopMakes = await _context.VehicleMakes
                        .OrderBy(m => m.Name)
                        .Take(10)
                        .Select(m => m.Name)
                        .ToListAsync()
                }
            };

            return Ok(stats);
        }

        /// <summary>
        /// Clears all vehicle data.
        /// </summary>
        [HttpDelete("clear")]
        // [Authorize(Policy = "AdminOnly")] // Uncomment in production!
        public async Task<IActionResult> ClearVehicleData()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM VehicleYearMakeModels");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM VehicleYearMakes");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM VehicleModels");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM VehicleMakes");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM VehicleYears");

                return Ok(new { message = "All vehicle data cleared" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing vehicle data");
                return StatusCode(500, new { message = "Error clearing vehicle data", error = ex.Message });
            }
        }
    }
}
