using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.Services;

namespace LastCallMotorAuctions.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiclesController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;
        private readonly ILogger<VehiclesController> _logger;

        public VehiclesController(IVehicleService vehicleService, ILogger<VehiclesController> logger)
        {
            _vehicleService = vehicleService;
            _logger = logger;
        }

        /// <summary>
        /// Get all available years for vehicle selection.
        /// </summary>
        [HttpGet("years")]
        public async Task<IActionResult> GetYears()
        {
            var years = await _vehicleService.GetYearsAsync();
            return Ok(years);
        }

        /// <summary>
        /// Get all makes available for a specific year.
        /// </summary>
        [HttpGet("years/{year:int}/makes")]
        public async Task<IActionResult> GetMakesByYear(short year)
        {
            var makes = await _vehicleService.GetMakesByYearAsync(year);
            return Ok(makes);
        }

        /// <summary>
        /// Get all models available for a specific year and make combination.
        /// </summary>
        [HttpGet("years/{year:int}/makes/{makeId:int}/models")]
        public async Task<IActionResult> GetModelsByYearAndMake(short year, int makeId)
        {
            var models = await _vehicleService.GetModelsByYearAndMakeAsync(year, makeId);
            return Ok(models);
        }

        /// <summary>
        /// Get all makes (useful for admin or general browsing).
        /// </summary>
        [HttpGet("makes")]
        public async Task<IActionResult> GetAllMakes()
        {
            var makes = await _vehicleService.GetAllMakesAsync();
            return Ok(makes);
        }

        /// <summary>
        /// Get all models for a specific make (useful for admin or general browsing).
        /// </summary>
        [HttpGet("makes/{makeId:int}/models")]
        public async Task<IActionResult> GetModelsByMake(int makeId)
        {
            var models = await _vehicleService.GetModelsByMakeAsync(makeId);
            return Ok(models);
        }
    }
}
