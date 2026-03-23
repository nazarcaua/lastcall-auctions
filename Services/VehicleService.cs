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

        /// <summary>
        /// Calculate a vehicle value estimate with repair cost breakdown.
        /// </summary>
        public async Task<VehicleEstimateResponseDto?> GetValueEstimateAsync(VehicleEstimateRequestDto request)
        {
            var make = await _context.VehicleMakes.FindAsync(request.MakeId);
            var model = await _context.VehicleModels.FindAsync(request.ModelId);

            if (make == null || model == null)
                return null;

            int currentYear = DateTime.Now.Year;
            int age = Math.Max(0, currentYear - request.Year);

            decimal baseNewPrice = GetMakeBasePrice(make.Name);
            decimal retainedValue = CalculateDepreciation(age);
            decimal baseValue = Math.Round(baseNewPrice * retainedValue / 500m) * 500m;

            decimal mileageAdjustment = 0m;
            if (request.Mileage.HasValue && age > 0)
            {
                int averageMileage = age * 20000;
                int diff = request.Mileage.Value - averageMileage;
                mileageAdjustment = Math.Round(-diff * 0.04m / 50m) * 50m;
                decimal cap = baseValue * 0.20m;
                mileageAdjustment = Math.Max(-cap, Math.Min(cap, mileageAdjustment));
            }

            decimal provinceMultiplier = GetProvinceLaborMultiplier(request.Province);
            var repairEstimates = new List<RepairEstimateDto>();
            decimal totalRepairCost = 0m;
            decimal issuesDeduction = 0m;

            foreach (var issue in request.Issues)
            {
                var (baseCost, timeEstimate) = GetRepairCost(issue.Category, issue.Severity);
                decimal parts = Math.Round(baseCost * 0.40m / 25m) * 25m;
                decimal labor = Math.Round(baseCost * 0.60m * provinceMultiplier / 25m) * 25m;
                decimal total = parts + labor;
                totalRepairCost += total;

                decimal deductionMultiplier = issue.Severity switch
                {
                    "Minor" => 0.50m,
                    "Moderate" => 0.85m,
                    "Severe" => 1.20m,
                    _ => 0.70m
                };
                issuesDeduction += total * deductionMultiplier;

                repairEstimates.Add(new RepairEstimateDto
                {
                    Category = issue.Category,
                    Issue = issue.Description,
                    Severity = issue.Severity,
                    PartsCost = parts,
                    LaborCost = labor,
                    TotalCost = total,
                    RepairTimeEstimate = timeEstimate
                });
            }

            issuesDeduction = Math.Round(issuesDeduction / 50m) * 50m;
            decimal estimatedValue = Math.Max(0m, baseValue + mileageAdjustment - issuesDeduction);
            decimal valueAfterRepairs = estimatedValue + issuesDeduction;

            string recommendation = totalRepairCost == 0
                ? "No issues reported — your vehicle is in good condition."
                : totalRepairCost < issuesDeduction
                    ? "Repairing is worth it — repair costs are less than the value you'd recover."
                    : totalRepairCost < issuesDeduction * 1.5m
                        ? "Marginal — repair costs are close to the value recovered. Consider your situation carefully."
                        : "Consider selling as-is — repair costs significantly exceed the value gained.";

            return new VehicleEstimateResponseDto
            {
                VehicleName = $"{request.Year} {make.Name} {model.Name}",
                Year = request.Year,
                Make = make.Name,
                Model = model.Name,
                BaseValue = baseValue,
                MileageAdjustment = mileageAdjustment,
                IssuesDeduction = issuesDeduction,
                EstimatedValue = estimatedValue,
                ValueRangeLow = Math.Round(estimatedValue * 0.92m / 500m) * 500m,
                ValueRangeHigh = Math.Round(estimatedValue * 1.08m / 500m) * 500m,
                TotalRepairCost = totalRepairCost,
                ValueAfterRepairs = valueAfterRepairs,
                Recommendation = recommendation,
                RepairEstimates = repairEstimates
            };
        }

        private static decimal GetMakeBasePrice(string makeName)
        {
            return makeName.ToLower() switch
            {
                "acura"           => 42000m,
                "alfa romeo"      => 50000m,
                "audi"            => 65000m,
                "bentley"         => 220000m,
                "bmw"             => 68000m,
                "buick"           => 38000m,
                "cadillac"        => 60000m,
                "chevrolet"       => 30000m,
                "chrysler"        => 35000m,
                "dodge"           => 33000m,
                "ferrari"         => 280000m,
                "fiat"            => 22000m,
                "ford"            => 34000m,
                "genesis"         => 50000m,
                "gmc"             => 40000m,
                "honda"           => 30000m,
                "hyundai"         => 26000m,
                "infiniti"        => 50000m,
                "jaguar"          => 72000m,
                "jeep"            => 40000m,
                "kia"             => 25000m,
                "lamborghini"     => 280000m,
                "land rover"      => 78000m,
                "lexus"           => 58000m,
                "lincoln"         => 58000m,
                "maserati"        => 105000m,
                "mazda"           => 29000m,
                "mercedes-benz"   => 70000m,
                "mini"            => 36000m,
                "mitsubishi"      => 24000m,
                "nissan"          => 28000m,
                "porsche"         => 95000m,
                "ram"             => 40000m,
                "rolls-royce"     => 380000m,
                "subaru"          => 31000m,
                "tesla"           => 62000m,
                "toyota"          => 33000m,
                "volkswagen"      => 36000m,
                "volvo"           => 55000m,
                _                 => 30000m
            };
        }

        private static decimal CalculateDepreciation(int age)
        {
            decimal value = 1.0m;
            for (int i = 0; i < age; i++)
            {
                value *= i switch
                {
                    0     => 0.80m,
                    1 or 2 => 0.85m,
                    3 or 4 or 5 => 0.88m,
                    6 or 7 or 8 or 9 => 0.92m,
                    _ => 0.95m
                };
            }
            return Math.Max(0.06m, value);
        }

        private static decimal GetProvinceLaborMultiplier(string province)
        {
            return province.ToUpper() switch
            {
                "AB" => 1.10m,
                "BC" => 1.15m,
                "MB" => 0.90m,
                "NB" => 0.95m,
                "NL" => 1.10m,
                "NS" => 0.95m,
                "NT" => 1.30m,
                "NU" => 1.35m,
                "ON" => 1.10m,
                "PE" => 0.90m,
                "QC" => 0.95m,
                "SK" => 0.90m,
                "YT" => 1.25m,
                _    => 1.00m
            };
        }

        private static (decimal BaseCost, string TimeEstimate) GetRepairCost(string category, string severity)
        {
            var costs = category.ToLower() switch
            {
                "engine"       => (Minor: 350m,  Moderate: 1800m, Severe: 6500m,  TMinor: "1-2 hrs",    TModerate: "6-12 hrs",  TSevere: "3-5 days"),
                "transmission" => (Minor: 400m,  Moderate: 2200m, Severe: 5800m,  TMinor: "2-4 hrs",    TModerate: "6-10 hrs",  TSevere: "2-4 days"),
                "brakes"       => (Minor: 250m,  Moderate: 800m,  Severe: 1800m,  TMinor: "1-2 hrs",    TModerate: "2-4 hrs",   TSevere: "4-8 hrs"),
                "suspension"   => (Minor: 300m,  Moderate: 900m,  Severe: 2200m,  TMinor: "1-3 hrs",    TModerate: "3-6 hrs",   TSevere: "1-2 days"),
                "electrical"   => (Minor: 200m,  Moderate: 700m,  Severe: 2000m,  TMinor: "1-2 hrs",    TModerate: "3-5 hrs",   TSevere: "6-12 hrs"),
                "body"         => (Minor: 400m,  Moderate: 1200m, Severe: 3500m,  TMinor: "2-4 hrs",    TModerate: "1-2 days",  TSevere: "3-5 days"),
                "interior"     => (Minor: 150m,  Moderate: 600m,  Severe: 1800m,  TMinor: "1-2 hrs",    TModerate: "3-5 hrs",   TSevere: "1-2 days"),
                "ac/heating"   => (Minor: 200m,  Moderate: 800m,  Severe: 1600m,  TMinor: "1-2 hrs",    TModerate: "3-5 hrs",   TSevere: "6-10 hrs"),
                "exhaust"      => (Minor: 250m,  Moderate: 700m,  Severe: 1400m,  TMinor: "1-2 hrs",    TModerate: "2-4 hrs",   TSevere: "4-8 hrs"),
                "tires"        => (Minor: 600m,  Moderate: 1200m, Severe: 2400m,  TMinor: "30-60 min",  TModerate: "1-2 hrs",   TSevere: "2-4 hrs"),
                _              => (Minor: 200m,  Moderate: 800m,  Severe: 2000m,  TMinor: "1-2 hrs",    TModerate: "3-6 hrs",   TSevere: "1-2 days"),
            };

            return severity.ToLower() switch
            {
                "minor"    => (costs.Minor,    costs.TMinor),
                "moderate" => (costs.Moderate, costs.TModerate),
                "severe"   => (costs.Severe,   costs.TSevere),
                _          => (costs.Moderate, costs.TModerate)
            };
        }
    }
}
