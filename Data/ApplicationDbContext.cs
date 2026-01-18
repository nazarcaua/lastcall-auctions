using Microsoft.EntityFrameworkCore;

namespace LastCallMotorAuctions.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }

    // DB setters/getters here for models
}