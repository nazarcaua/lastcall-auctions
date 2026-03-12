using LastCallMotorAuctions.API.Models;
using Microsoft.AspNetCore.Identity;

namespace LastCallMotorAuctions.API.Data;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        const string email = "admin@gmail.com";
        const string password = "Adminaccount1";
        const string fullName = "Admin";

        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            // Ensure the Admin role is assigned even if the user already exists
            if (!await userManager.IsInRoleAsync(existing, "Admin"))
            {
                await userManager.AddToRoleAsync(existing, "Admin");
                logger.LogInformation("Added Admin role to existing user {Email}", email);
            }

            // Reset password to the expected value
            var token = await userManager.GeneratePasswordResetTokenAsync(existing);
            var resetResult = await userManager.ResetPasswordAsync(existing, token, password);
            if (resetResult.Succeeded)
            {
                logger.LogInformation("Reset admin account password for {Email}", email);
            }
            else
            {
                var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                logger.LogError("Failed to reset admin password: {Errors}", errors);
            }

            return;
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            StatusId = 1, // Active
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin");
            logger.LogInformation("Seeded admin account: {Email}", email);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to seed admin account: {Errors}", errors);
        }
    }
}
