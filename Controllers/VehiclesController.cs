using Microsoft.AspNetCore.Mvc;
using LastCallMotorAuctions.API.DTOs;
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
        /// Get makes, optionally filtered by year (query param).
        /// Used by the vehicle value estimator frontend.
        /// </summary>
        [HttpGet("makes")]
        public async Task<IActionResult> GetMakes([FromQuery] short? year)
        {
            var makes = year.HasValue
                ? await _vehicleService.GetMakesByYearAsync(year.Value)
                : await _vehicleService.GetAllMakesAsync();
            return Ok(makes);
        }

        /// <summary>
        /// Get models filtered by year and make (query params).
        /// Used by the vehicle value estimator frontend.
        /// </summary>
        [HttpGet("models")]
        public async Task<IActionResult> GetModels([FromQuery] short year, [FromQuery] int makeId)
        {
            var models = await _vehicleService.GetModelsByYearAndMakeAsync(year, makeId);
            return Ok(models);
        }

        /// <summary>
        /// Get all makes available for a specific year (path param route).
        /// </summary>
        [HttpGet("years/{year:int}/makes")]
        public async Task<IActionResult> GetMakesByYear(short year)
        {
            var makes = await _vehicleService.GetMakesByYearAsync(year);
            return Ok(makes);
        }

        /// <summary>
        /// Get all models available for a specific year and make combination (path param route).
        /// </summary>
        [HttpGet("years/{year:int}/makes/{makeId:int}/models")]
        public async Task<IActionResult> GetModelsByYearAndMake(short year, int makeId)
        {
            var models = await _vehicleService.GetModelsByYearAndMakeAsync(year, makeId);
            return Ok(models);
        }

        /// <summary>
        /// Get all models for a specific make.
        /// </summary>
        [HttpGet("makes/{makeId:int}/models")]
        public async Task<IActionResult> GetModelsByMake(int makeId)
        {
            var models = await _vehicleService.GetModelsByMakeAsync(makeId);
            return Ok(models);
        }

        /// <summary>
        /// Calculate a vehicle value estimate with repair cost breakdown.
        /// </summary>
        [HttpPost("value-estimate")]
        public async Task<IActionResult> GetValueEstimate([FromBody] VehicleEstimateRequestDto request)
        {
            var result = await _vehicleService.GetValueEstimateAsync(request);
            if (result == null)
                return NotFound("Vehicle make or model not found.");
            return Ok(result);
        }
    }
}

