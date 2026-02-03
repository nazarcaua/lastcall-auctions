using LastCallMotorAuctions.API.Data;
using LastCallMotorAuctions.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly ApplicationDbContext _context;

        public VehicleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<VehicleResponseDto> CreateVehicleAsync(CreateVehicleDto createVehicleDto, int sellerId)
        {
            // TODO: Implement vehicle creation
            throw new NotImplementedException();
        }

        public Task<VehicleResponseDto?> GetVehicleByIdAsync(int vehicleId)
        {
            // TODO: Implement get vehicle by ID
            throw new NotImplementedException();
        }

        public Task<List<VehicleResponseDto>> SearchVehicleAsync(string? make, string? model, int? year, decimal? minPrice, decimal? maxPrice)
        {
            // TODO: Implement vehicle search
            throw new NotImplementedException();
        }

        public Task<bool> VehicleVINAsync(string vin)
        {
            // TODO: Implement VIN validation via VINAudit API
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get all available years that have makes associated with them.
        /// </summary>
        public async Task<List<VehicleYearDto>> GetYearsAsync()
        {
            return await _context.VehicleYears
                .OrderByDescending(y => y.Year)
                .Select(y => new VehicleYearDto { Year = y.Year })
                .ToListAsync();
        }

        /// <summary>
        /// Get all makes available for a specific year.
        /// </summary>
        public async Task<List<VehicleMakeDto>> GetMakesByYearAsync(short year)
        {
            return await _context.VehicleYearMakes
                .Where(ym => ym.Year == year)
                .Select(ym => new VehicleMakeDto
                {
                    MakeId = ym.MakeId,
                    Name = ym.Make!.Name
                })
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Get all models available for a specific year and make combination.
        /// </summary>
        public async Task<List<VehicleModelDto>> GetModelsByYearAndMakeAsync(short year, int makeId)
        {
            return await _context.VehicleYearMakeModels
                .Where(ymm => ymm.YearMake!.Year == year && ymm.YearMake.MakeId == makeId)
                .Select(ymm => new VehicleModelDto
                {
                    ModelId = ymm.ModelId,
                    Name = ymm.Model!.Name
                })
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Get all makes (for admin purposes).
        /// </summary>
        public async Task<List<VehicleMakeDto>> GetAllMakesAsync()
        {
            return await _context.VehicleMakes
                .OrderBy(m => m.Name)
                .Select(m => new VehicleMakeDto
                {
                    MakeId = m.MakeId,
                    Name = m.Name
                })
                .ToListAsync();
        }

        /// <summary>
        /// Get all models for a specific make (for admin purposes).
        /// </summary>
        public async Task<List<VehicleModelDto>> GetModelsByMakeAsync(int makeId)
        {
            return await _context.VehicleModels
                .Where(m => m.MakeId == makeId)
                .OrderBy(m => m.Name)
                .Select(m => new VehicleModelDto
                {
                    ModelId = m.ModelId,
                    Name = m.Name
                })
                .ToListAsync();
        }
    }
}
