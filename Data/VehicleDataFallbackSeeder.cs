using LastCallMotorAuctions.API.Models;
using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Data
{
    /// <summary>
    /// Simple fallback seeder that populates vehicle data without external API calls.
    /// Use this if NHTSA seeder fails or times out.
    /// </summary>
    public static class VehicleDataFallbackSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            // Check if YearMakes already has data
            if (await context.VehicleYearMakes.AnyAsync())
            {
                logger?.LogInformation("VehicleYearMakes already has data. Skipping fallback seed.");
                return;
            }

            logger?.LogInformation("Running fallback vehicle data seeder...");

            // Ensure years exist (1990-2026)
            var existingYears = await context.VehicleYears.Select(y => y.Year).ToListAsync();
            var yearsToAdd = Enumerable.Range(1990, 37)
                .Select(y => (short)y)
                .Where(y => !existingYears.Contains(y))
                .Select(y => new VehicleYear { Year = y })
                .ToList();

            if (yearsToAdd.Any())
            {
                await context.VehicleYears.AddRangeAsync(yearsToAdd);
                await context.SaveChangesAsync();
                logger?.LogInformation("Added {Count} missing years", yearsToAdd.Count);
            }

            // Define makes and their models
            var makeModels = new Dictionary<string, string[]>
            {
                ["Acura"] = new[] { "ILX", "Integra", "MDX", "NSX", "RDX", "TLX", "RSX", "TSX" },
                ["Audi"] = new[] { "A3", "A4", "A5", "A6", "A7", "A8", "Q3", "Q5", "Q7", "Q8", "e-tron", "R8", "RS6", "S4" },
                ["BMW"] = new[] { "2 Series", "3 Series", "4 Series", "5 Series", "7 Series", "X1", "X3", "X5", "X7", "M3", "M5", "Z4", "i4", "iX" },
                ["Chevrolet"] = new[] { "Camaro", "Corvette", "Cruze", "Equinox", "Impala", "Malibu", "Silverado", "Suburban", "Tahoe", "Traverse", "Trailblazer", "Colorado" },
                ["Dodge"] = new[] { "Challenger", "Charger", "Durango", "Grand Caravan", "Journey", "Viper", "Ram 1500" },
                ["Ford"] = new[] { "Bronco", "Edge", "Escape", "Explorer", "F-150", "F-250", "Fiesta", "Focus", "Fusion", "Mustang", "Ranger", "Expedition", "Maverick" },
                ["GMC"] = new[] { "Acadia", "Canyon", "Sierra", "Terrain", "Yukon", "Hummer EV" },
                ["Honda"] = new[] { "Accord", "Civic", "CR-V", "HR-V", "Fit", "Insight", "Odyssey", "Passport", "Pilot", "Ridgeline", "S2000" },
                ["Hyundai"] = new[] { "Elantra", "Ioniq", "Kona", "Palisade", "Santa Fe", "Sonata", "Tucson", "Veloster", "Genesis" },
                ["Jeep"] = new[] { "Cherokee", "Compass", "Gladiator", "Grand Cherokee", "Renegade", "Wrangler", "Wagoneer" },
                ["Kia"] = new[] { "Forte", "K5", "Niro", "Seltos", "Sorento", "Soul", "Sportage", "Stinger", "Telluride", "EV6" },
                ["Lexus"] = new[] { "ES", "GS", "GX", "IS", "LC", "LS", "LX", "NX", "RC", "RX", "UX" },
                ["Mazda"] = new[] { "CX-3", "CX-30", "CX-5", "CX-9", "Mazda3", "Mazda6", "MX-5 Miata", "RX-7", "RX-8" },
                ["Mercedes-Benz"] = new[] { "A-Class", "C-Class", "E-Class", "S-Class", "GLA", "GLC", "GLE", "GLS", "AMG GT", "EQS", "G-Class" },
                ["Nissan"] = new[] { "350Z", "370Z", "Altima", "Armada", "Frontier", "GTR", "Kicks", "Leaf", "Maxima", "Murano", "Pathfinder", "Rogue", "Sentra", "Titan", "Versa" },
                ["Porsche"] = new[] { "911", "918", "Boxster", "Cayenne", "Cayman", "Macan", "Panamera", "Taycan" },
                ["Subaru"] = new[] { "Ascent", "BRZ", "Crosstrek", "Forester", "Impreza", "Legacy", "Outback", "WRX" },
                ["Tesla"] = new[] { "Model 3", "Model S", "Model X", "Model Y", "Cybertruck", "Roadster" },
                ["Toyota"] = new[] { "4Runner", "86", "Avalon", "Camry", "Corolla", "GR Supra", "Highlander", "Land Cruiser", "Prius", "RAV4", "Sequoia", "Sienna", "Tacoma", "Tundra", "Venza" },
                ["Volkswagen"] = new[] { "Atlas", "Golf", "GTI", "ID.4", "Jetta", "Passat", "Taos", "Tiguan", "Arteon" },
                ["Volvo"] = new[] { "S60", "S90", "V60", "V90", "XC40", "XC60", "XC90", "C40" }
            };

            // Get existing makes
            var existingMakes = await context.VehicleMakes.ToDictionaryAsync(m => m.Name.ToLower(), m => m);

            // Add missing makes
            foreach (var makeName in makeModels.Keys)
            {
                if (!existingMakes.ContainsKey(makeName.ToLower()))
                {
                    var newMake = new VehicleMake { Name = makeName };
                    context.VehicleMakes.Add(newMake);
                    existingMakes[makeName.ToLower()] = newMake;
                }
            }
            await context.SaveChangesAsync();
            logger?.LogInformation("Ensured {Count} makes exist", makeModels.Count);

            // Refresh make IDs after save
            existingMakes = await context.VehicleMakes.ToDictionaryAsync(m => m.Name.ToLower(), m => m);

            // Add models
            var existingModels = await context.VehicleModels
                .Select(m => $"{m.MakeId}_{m.Name.ToLower()}")
                .ToHashSetAsync();

            var modelsToAdd = new List<VehicleModel>();
            foreach (var (makeName, models) in makeModels)
            {
                var make = existingMakes[makeName.ToLower()];
                foreach (var modelName in models)
                {
                    var key = $"{make.MakeId}_{modelName.ToLower()}";
                    if (!existingModels.Contains(key))
                    {
                        modelsToAdd.Add(new VehicleModel { MakeId = make.MakeId, Name = modelName });
                        existingModels.Add(key);
                    }
                }
            }

            if (modelsToAdd.Any())
            {
                await context.VehicleModels.AddRangeAsync(modelsToAdd);
                await context.SaveChangesAsync();
                logger?.LogInformation("Added {Count} models", modelsToAdd.Count);
            }

            // Create model lookup
            var modelLookup = await context.VehicleModels
                .ToDictionaryAsync(m => $"{m.MakeId}_{m.Name.ToLower()}", m => m.ModelId);

            // Add YearMakes and YearMakeModels for all years 2000-2026 (covers most common vehicles)
            var years = Enumerable.Range(2000, 27).Select(y => (short)y).ToList();
            
            var existingYearMakes = await context.VehicleYearMakes
                .Select(ym => $"{ym.Year}_{ym.MakeId}")
                .ToHashSetAsync();

            var yearMakesToAdd = new List<VehicleYearMake>();
            
            foreach (var year in years)
            {
                foreach (var (makeName, _) in makeModels)
                {
                    var make = existingMakes[makeName.ToLower()];
                    var key = $"{year}_{make.MakeId}";
                    if (!existingYearMakes.Contains(key))
                    {
                        yearMakesToAdd.Add(new VehicleYearMake { Year = year, MakeId = make.MakeId });
                        existingYearMakes.Add(key);
                    }
                }
            }

            if (yearMakesToAdd.Any())
            {
                await context.VehicleYearMakes.AddRangeAsync(yearMakesToAdd);
                await context.SaveChangesAsync();
                logger?.LogInformation("Added {Count} year-make combinations", yearMakesToAdd.Count);
            }

            // Refresh YearMakes with IDs
            var yearMakeLookup = await context.VehicleYearMakes
                .ToDictionaryAsync(ym => $"{ym.Year}_{ym.MakeId}", ym => ym.YearMakeId);

            // Add YearMakeModels
            var existingYearMakeModels = await context.VehicleYearMakeModels
                .Select(ymm => $"{ymm.YearMakeId}_{ymm.ModelId}")
                .ToHashSetAsync();

            var yearMakeModelsToAdd = new List<VehicleYearMakeModel>();

            foreach (var year in years)
            {
                foreach (var (makeName, models) in makeModels)
                {
                    var make = existingMakes[makeName.ToLower()];
                    var yearMakeKey = $"{year}_{make.MakeId}";
                    
                    if (!yearMakeLookup.TryGetValue(yearMakeKey, out var yearMakeId))
                        continue;

                    foreach (var modelName in models)
                    {
                        var modelKey = $"{make.MakeId}_{modelName.ToLower()}";
                        if (!modelLookup.TryGetValue(modelKey, out var modelId))
                            continue;

                        var ymmKey = $"{yearMakeId}_{modelId}";
                        if (!existingYearMakeModels.Contains(ymmKey))
                        {
                            yearMakeModelsToAdd.Add(new VehicleYearMakeModel 
                            { 
                                YearMakeId = yearMakeId, 
                                ModelId = modelId 
                            });
                            existingYearMakeModels.Add(ymmKey);
                        }
                    }
                }
            }

            if (yearMakeModelsToAdd.Any())
            {
                await context.VehicleYearMakeModels.AddRangeAsync(yearMakeModelsToAdd);
                await context.SaveChangesAsync();
                logger?.LogInformation("Added {Count} year-make-model combinations", yearMakeModelsToAdd.Count);
            }

            logger?.LogInformation("Fallback vehicle data seeding complete!");
        }
    }
}
