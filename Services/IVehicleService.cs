using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public interface IVehicleService
    {
        Task<VehicleResponseDto> CreateVehicleAsync(CreateVehicleDto createVehicleDto, int sellerId);
        Task<VehicleResponseDto?> GetVehicleByIdAsync(int vehicleId);
        Task<List<VehicleResponseDto>> SearchVehicleAsync(string? make, string? model, int? year, decimal? minPrice, decimal? maxPrice);
        Task<bool> VehicleVINAsync(string vin);

        // Year/Make/Model lookup methods for cascading dropdowns
        Task<List<VehicleYearDto>> GetYearsAsync();
        Task<List<VehicleMakeDto>> GetMakesByYearAsync(short year);
        Task<List<VehicleModelDto>> GetModelsByYearAndMakeAsync(short year, int makeId);
        
        // Admin methods for managing vehicle data
        Task<List<VehicleMakeDto>> GetAllMakesAsync();
        Task<List<VehicleModelDto>> GetModelsByMakeAsync(int makeId);
    }
}
