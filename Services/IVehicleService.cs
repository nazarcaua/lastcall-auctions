using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public interface IVehicleService
    {
        Task<VehicleResponseDto> CreateVehicleAsync(CreateVehicleDto createVehicleDto, int sellerId);
        Task<VehicleResponseDto?> GetVehicleByIdAsync(int vehicleId);
        Task<List<VehicleResponseDto>> SearchVehicleAsync(string? make, string? model, int? year, decimal? minPrice, decimal? maxPrice);
        Task<bool> VehicleVINAsync(string vin);
    }
}
