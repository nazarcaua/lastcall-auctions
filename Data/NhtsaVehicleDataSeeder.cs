using LastCallMotorAuctions.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LastCallMotorAuctions.API.Data
{
    /// <summary>
    /// Seeds vehicle data from NHTSA (National Highway Traffic Safety Administration) vPIC API.
    /// This is the official US government source for vehicle make/model data.
    /// API Docs: https://vpic.nhtsa.dot.gov/api/
    /// </summary>
    public static class NhtsaVehicleDataSeeder
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Popular makes to seed (keeps database manageable while covering most vehicles)
        private static readonly string[] PopularMakes =
        [
            "Acura", "Alfa Romeo", "Aston Martin", "Audi", "Bentley", "BMW", "Buick",
            "Cadillac", "Chevrolet", "Chrysler", "Dodge", "Ferrari", "Fiat", "Ford",
            "Genesis", "GMC", "Honda", "Hyundai", "Infiniti", "Jaguar", "Jeep", "Kia",
            "Lamborghini", "Land Rover", "Lexus", "Lincoln", "Maserati", "Mazda",
            "McLaren", "Mercedes-Benz", "Mini", "Mitsubishi", "Nissan", "Porsche",
            "Ram", "Rivian", "Rolls-Royce", "Subaru", "Tesla", "Toyota", "Volkswagen", "Volvo"
        ];

        // Cache for model lookups to avoid duplicate API calls
        private static readonly Dictionary<string, List<NhtsaModel>> _modelCache = new();

        public static async Task SeedFromNhtsaAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            // Check if data already exists
            if (await context.VehicleYears.AnyAsync())
            {
                logger?.LogInformation("Vehicle data already exists. Skipping NHTSA seed.");
                return;
            }

            _modelCache.Clear();
            logger?.LogInformation("Starting NHTSA vehicle data seeding (this may take several minutes)...");

            try
            {
                // Step 1: Seed years (1980-2026 to include classic vehicles)
                var startYear = 1980;
                var endYear = DateTime.Now.Year + 1;
                
                var years = Enumerable.Range(startYear, endYear - startYear + 1)
                    .Select(y => new VehicleYear { Year = (short)y })
                    .ToList();

                await context.VehicleYears.AddRangeAsync(years);
                await context.SaveChangesAsync();
                logger?.LogInformation("Seeded {Count} years ({Start}-{End})", years.Count, startYear, endYear);

                // Step 2: Get all makes from NHTSA and filter to popular ones
                logger?.LogInformation("Fetching makes from NHTSA...");
                var nhtsaMakes = await GetMakesFromNhtsaAsync(logger);
                var filteredMakes = nhtsaMakes
                    .Where(m => PopularMakes.Any(pm => pm.Equals(m.MakeName, StringComparison.OrdinalIgnoreCase)))
                    .DistinctBy(m => m.MakeName)
                    .ToList();

                logger?.LogInformation("Found {Count} matching makes from NHTSA", filteredMakes.Count);

                // Add makes to database
                var makes = filteredMakes.Select(m => new VehicleMake { Name = m.MakeName }).ToList();
                await context.VehicleMakes.AddRangeAsync(makes);
                await context.SaveChangesAsync();
                logger?.LogInformation("Seeded {Count} makes", makes.Count);

                // Create lookup dictionaries
                var makeNhtsaIdMap = filteredMakes.ToDictionary(m => m.MakeName.ToLower(), m => m.MakeId);

                // Step 3: For each make AND year, get models from NHTSA (year-specific data)
                var allModels = new Dictionary<string, VehicleModel>();
                var yearMakeModelsData = new List<(short Year, int MakeId, string ModelName)>();
                var yearMakesWithModels = new HashSet<string>();

                var totalYears = endYear - startYear + 1;
                
                foreach (var make in makes)
                {
                    if (!makeNhtsaIdMap.TryGetValue(make.Name.ToLower(), out var nhtsaMakeId))
                        continue;

                    logger?.LogInformation("Fetching models for {Make} across {Years} years...", make.Name, totalYears);

                    for (short year = (short)startYear; year <= endYear; year++)
                    {
                        var models = await GetModelsForMakeYearAsync(nhtsaMakeId, year, logger);
                        
                        if (models.Count > 0)
                        {
                            yearMakesWithModels.Add($"{year}_{make.MakeId}");
                            
                            foreach (var model in models)
                            {
                                var modelKey = $"{make.MakeId}_{model.ModelName}";
                                if (!allModels.ContainsKey(modelKey))
                                {
                                    allModels[modelKey] = new VehicleModel
                                    {
                                        MakeId = make.MakeId,
                                        Name = model.ModelName
                                    };
                                }
                                
                                yearMakeModelsData.Add((year, make.MakeId, model.ModelName));
                            }
                        }
                    }
                    
                    // Log progress
                    logger?.LogInformation("  {Make}: found {ModelCount} unique models across available years", 
                        make.Name, allModels.Values.Count(m => m.MakeId == make.MakeId));
                }

                // Save all unique models
                await context.VehicleModels.AddRangeAsync(allModels.Values);
                await context.SaveChangesAsync();
                logger?.LogInformation("Seeded {Count} unique models", allModels.Count);

                // Create model lookup
                var modelDict = await context.VehicleModels
                    .ToDictionaryAsync(m => $"{m.MakeId}_{m.Name}", m => m.ModelId);

                // Step 4: Create YearMake entries ONLY for year-make combinations that have models
                var yearMakes = yearMakesWithModels
                    .Select(key =>
                    {
                        var parts = key.Split('_');
                        return new VehicleYearMake
                        {
                            Year = short.Parse(parts[0]),
                            MakeId = int.Parse(parts[1])
                        };
                    })
                    .ToList();

                await context.VehicleYearMakes.AddRangeAsync(yearMakes);
                await context.SaveChangesAsync();
                logger?.LogInformation("Seeded {Count} year-make combinations", yearMakes.Count);

                // Create year-make lookup
                var yearMakeDict = await context.VehicleYearMakes
                    .ToDictionaryAsync(ym => $"{ym.Year}_{ym.MakeId}", ym => ym.YearMakeId);

                // Step 5: Create YearMakeModel entries (only for specific years each model was available)
                var yearMakeModels = new List<VehicleYearMakeModel>();
                var addedCombos = new HashSet<string>();

                foreach (var (year, makeId, modelName) in yearMakeModelsData)
                {
                    var ymKey = $"{year}_{makeId}";
                    var modelKey = $"{makeId}_{modelName}";
                    var comboKey = $"{ymKey}_{modelKey}";

                    if (!yearMakeDict.TryGetValue(ymKey, out var yearMakeId))
                        continue;
                    if (!modelDict.TryGetValue(modelKey, out var modelId))
                        continue;
                    if (addedCombos.Contains(comboKey))
                        continue;

                    addedCombos.Add(comboKey);
                    yearMakeModels.Add(new VehicleYearMakeModel
                    {
                        YearMakeId = yearMakeId,
                        ModelId = modelId
                    });
                }

                await context.VehicleYearMakeModels.AddRangeAsync(yearMakeModels);
                await context.SaveChangesAsync();
                logger?.LogInformation("Seeded {Count} year-make-model combinations", yearMakeModels.Count);

                logger?.LogInformation("NHTSA vehicle data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error seeding NHTSA vehicle data");
                throw;
            }
        }

        private static async Task<List<NhtsaMake>> GetMakesFromNhtsaAsync(ILogger? logger)
        {
            try
            {
                var carUrl = "https://vpic.nhtsa.dot.gov/api/vehicles/GetMakesForVehicleType/car?format=json";
                var carResponse = await _httpClient.GetStringAsync(carUrl);
                var carResult = JsonSerializer.Deserialize<NhtsaResponse<NhtsaMake>>(carResponse, _jsonOptions);
                
                var truckUrl = "https://vpic.nhtsa.dot.gov/api/vehicles/GetMakesForVehicleType/truck?format=json";
                var truckResponse = await _httpClient.GetStringAsync(truckUrl);
                var truckResult = JsonSerializer.Deserialize<NhtsaResponse<NhtsaMake>>(truckResponse, _jsonOptions);

                var allMakes = new List<NhtsaMake>();
                if (carResult?.Results != null) allMakes.AddRange(carResult.Results);
                if (truckResult?.Results != null) allMakes.AddRange(truckResult.Results);

                return allMakes.DistinctBy(m => m.MakeName).ToList();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to get makes from NHTSA, using fallback");
                return PopularMakes.Select((name, index) => new NhtsaMake 
                { 
                    MakeId = index + 1, 
                    MakeName = name 
                }).ToList();
            }
        }

        private static async Task<List<NhtsaModel>> GetModelsForMakeYearAsync(int makeId, short year, ILogger? logger)
        {
            var cacheKey = $"{makeId}_{year}";
            if (_modelCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            try
            {
                var url = $"https://vpic.nhtsa.dot.gov/api/vehicles/GetModelsForMakeIdYear/makeId/{makeId}/modelyear/{year}?format=json";
                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<NhtsaResponse<NhtsaModel>>(response, _jsonOptions);
                
                var models = result?.Results ?? [];
                
                var cleanedModels = models
                    .Where(m => !string.IsNullOrWhiteSpace(m.ModelName))
                    .Select(m => new NhtsaModel { ModelId = m.ModelId, ModelName = m.ModelName.Trim() })
                    .DistinctBy(m => m.ModelName)
                    .OrderBy(m => m.ModelName)
                    .ToList();

                _modelCache[cacheKey] = cleanedModels;
                return cleanedModels;
            }
            catch
            {
                _modelCache[cacheKey] = [];
                return [];
            }
        }

        private class NhtsaResponse<T>
        {
            public int Count { get; set; }
            public string? Message { get; set; }
            public List<T> Results { get; set; } = [];
        }

        private class NhtsaMake
        {
            [JsonPropertyName("MakeId")]
            public int MakeId { get; set; }
            
            [JsonPropertyName("MakeName")]
            public string MakeName { get; set; } = string.Empty;
        }

        private class NhtsaModel
        {
            [JsonPropertyName("Model_ID")]
            public int ModelId { get; set; }
            
            [JsonPropertyName("Model_Name")]
            public string ModelName { get; set; } = string.Empty;
        }
    }
}
