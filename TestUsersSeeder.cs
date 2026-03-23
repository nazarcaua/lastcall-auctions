using LastCallMotorAuctions.API.Models;
using Microsoft.AspNetCore.Identity;

namespace LastCallMotorAuctions.API.Data;

public static class TestUsersSeeder
{
    public static async Task SeedTestUsersAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // ----- SELLER -----
        const string sellerEmail = "seller@gmail.com";
        const string sellerPassword = "Seller123!";
        const string sellerName = "Test Seller";

        var existingSeller = await userManager.FindByEmailAsync(sellerEmail);
        if (existingSeller == null)
        {
            var seller = new User
            {
                UserName = sellerEmail,
                Email = sellerEmail,
                FullName = sellerName,
                StatusId = 1,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(seller, sellerPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(seller, "Seller");
                logger.LogInformation("Seeded seller account: {Email}", sellerEmail);
            }
        }

        // ----- BUYER -----
        const string buyerEmail = "buyer@gmail.com";
        const string buyerPassword = "Buyer123!";
        const string buyerName = "Test Buyer";

        var existingBuyer = await userManager.FindByEmailAsync(buyerEmail);
        if (existingBuyer == null)
        {
            var buyer = new User
            {
                UserName = buyerEmail,
                Email = buyerEmail,
                FullName = buyerName,
                StatusId = 1,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(buyer, buyerPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(buyer, "Buyer");
                logger.LogInformation("Seeded buyer account: {Email}", buyerEmail);
            }
        }
    }
}
