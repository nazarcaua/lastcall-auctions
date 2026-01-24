using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.Services
{
    public class VehicleService : IVehicleService
    {
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
    }
}
