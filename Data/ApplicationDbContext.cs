using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LastCallMotorAuctions.API.Models;

namespace LastCallMotorAuctions.API.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }

    // Note: Users, Roles, and UserRoles are now handled by Identity
    // ApplicationUser replaces the old User model


    // DbSets for models
    public DbSet<VehicleYear> VehicleYears { get; set; } = null!;
    public DbSet<VehicleMake> VehicleMakes { get; set; } = null!;
    public DbSet<VehicleModel> VehicleModels { get; set; } = null!;
    public DbSet<VehicleYearMake> VehicleYearMakes { get; set; } = null!;
    public DbSet<VehicleYearMakeModel> VehicleYearMakeModels { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;

    public DbSet<Listing> Listings { get; set; } = null!;
    public DbSet<Auction> Auctions { get; set; } = null!;
    public DbSet<Bid> Bids { get; set; } = null!;
    public DbSet<AuctionResult> AuctionResults { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;

    // Lookup tables
    public DbSet<UserStatus> UserStatuses { get; set; } = null!;
    public DbSet<ListingStatus> ListingStatuses { get; set; } = null!;
    public DbSet<AuctionStatus> AuctionStatuses { get; set; } = null!;
    public DbSet<PaymentStatus> PaymentStatuses { get; set; } = null!;
    public DbSet<Currency> Currencies { get; set; } = null!;
    public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
    public DbSet<NotificationType> NotificationTypes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<User>(b =>
        {
            b.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            b.Property(u => u.StatusId).IsRequired();
            b.HasIndex(u => u.Email).IsUnique();
            b.HasOne(u => u.Status).WithMany().HasForeignKey(u => u.StatusId).OnDelete(DeleteBehavior.Restrict);
        });

        // Lookups
        modelBuilder.Entity<UserStatus>(b =>
        {
            b.HasKey(x => x.StatusId);
            b.Property(x => x.Name).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
            b.HasData(
                new UserStatus { StatusId = 1, Name = "Active" },
                new UserStatus { StatusId = 2, Name = "Suspended" }
            );
        });

        modelBuilder.Entity<ListingStatus>(b =>
        {
            b.HasKey(x => x.StatusId);
            b.Property(x => x.Name).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
            b.HasData(
                new ListingStatus { StatusId = 1, Name = "Draft" },
                new ListingStatus { StatusId = 2, Name = "Active" },
                new ListingStatus { StatusId = 3, Name = "Archived" }
            );
        });

        modelBuilder.Entity<AuctionStatus>(b =>
        {
            b.HasKey(x => x.StatusId);
            b.Property(x => x.Name).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
            b.HasData(
                new AuctionStatus { StatusId = 1, Name = "Scheduled" },
                new AuctionStatus { StatusId = 2, Name = "Active" },
                new AuctionStatus { StatusId = 3, Name = "Ended" },
                new AuctionStatus { StatusId = 4, Name = "Canceled" }
            );
        });

        modelBuilder.Entity<PaymentStatus>(b =>
        {
            b.HasKey(x => x.StatusId);
            b.Property(x => x.Name).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
            b.HasData(
                new PaymentStatus { StatusId = 1, Name = "Pending" },
                new PaymentStatus { StatusId = 2, Name = "Captured" },
                new PaymentStatus { StatusId = 3, Name = "Failed" },
                new PaymentStatus { StatusId = 4, Name = "Voided" }
            );
        });

        modelBuilder.Entity<Currency>(b =>
        {
            b.HasKey(x => x.CurrencyCode);
            b.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
            b.Property(x => x.Name).HasMaxLength(40).IsRequired();
            b.HasData(
                new Currency { CurrencyCode = "CAD", Name = "Canadian Dollar" },
                new Currency { CurrencyCode = "USD", Name = "US Dollar" }
            );
        });

        modelBuilder.Entity<PaymentMethod>(b =>
        {
            b.HasKey(x => x.MethodId);
            b.Property(x => x.Name).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
            b.HasData(
                new PaymentMethod { MethodId = 1, Name = "Card" },
                new PaymentMethod { MethodId = 2, Name = "ETransfer" },
                new PaymentMethod { MethodId = 3, Name = "Cash" }
            );
        });

        modelBuilder.Entity<NotificationType>(b =>
        {
            b.HasKey(x => x.TypeId);
            b.Property(x => x.Name).HasMaxLength(30).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
            b.HasData(
                new NotificationType { TypeId = 1, Name = "BidPlaced" },
                new NotificationType { TypeId = 2, Name = "Outbid" },
                new NotificationType { TypeId = 3, Name = "AuctionEnded" },
                new NotificationType { TypeId = 4, Name = "PaymentStatusChanged" }
            );
        });

        // Vehicles
        modelBuilder.Entity<VehicleYear>(b =>
        {
            b.HasKey(x => x.Year);
            b.Property(x => x.Year).ValueGeneratedNever();
        });

        modelBuilder.Entity<VehicleMake>(b =>
        {
            b.HasKey(x => x.MakeId);
            b.Property(x => x.Name).HasMaxLength(60).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<VehicleModel>(b =>
        {
            b.HasKey(x => x.ModelId);
            b.Property(x => x.Name).HasMaxLength(60).IsRequired();
            b.HasIndex(x => new { x.MakeId, x.Name }).IsUnique();
            b.HasOne(x => x.Make).WithMany(m => m.Models).HasForeignKey(x => x.MakeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VehicleYearMake>(b =>
        {
            b.HasKey(x => x.YearMakeId);
            b.HasIndex(x => new { x.Year, x.MakeId }).IsUnique();
            b.HasOne(x => x.VehicleYear).WithMany(y => y.YearMakes).HasForeignKey(x => x.Year).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Make).WithMany(m => m.YearMakes).HasForeignKey(x => x.MakeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VehicleYearMakeModel>(b =>
        {
            b.HasKey(x => x.YearMakeModelId);
            b.HasIndex(x => new { x.YearMakeId, x.ModelId }).IsUnique();
            b.HasOne(x => x.YearMake).WithMany(ym => ym.YearMakeModels).HasForeignKey(x => x.YearMakeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Model).WithMany(m => m.YearMakeModels).HasForeignKey(x => x.ModelId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Location>(b =>
        {
            b.HasKey(x => x.LocationId);
            b.Property(x => x.City).HasMaxLength(80).IsRequired();
            b.Property(x => x.Region).HasMaxLength(80);
            b.Property(x => x.Country).HasMaxLength(80).IsRequired();
            b.Property(x => x.PostalCode).HasMaxLength(20);
            b.HasIndex(x => new { x.City, x.Region, x.Country, x.PostalCode }).IsUnique();
        });

        // Listings
        modelBuilder.Entity<Listing>(b =>
        {
            b.HasKey(x => x.ListingId);
            b.Property(x => x.Title).HasMaxLength(120).IsRequired();
            b.Property(x => x.Description).HasColumnType("NVARCHAR(MAX)");
            b.Property(x => x.Year).IsRequired();
            b.Property(x => x.ConditionGrade).IsRequired();
            b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Make).WithMany().HasForeignKey(x => x.MakeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Model).WithMany().HasForeignKey(x => x.ModelId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
            b.HasCheckConstraint("CK_Listings_condition_grade", "ConditionGrade BETWEEN 1 AND 5");
            b.HasCheckConstraint("CK_Listings_year", "[Year] BETWEEN 1886 AND 2100");
        });

        // Auctions
        modelBuilder.Entity<Auction>(b =>
        {
            b.HasKey(x => x.AuctionId);
            b.Property(x => x.StartPrice).HasColumnType("decimal(18,2)").IsRequired();
            b.Property(x => x.ReservePrice).HasColumnType("decimal(18,2)");
            b.Property(x => x.StartTime).IsRequired();
            b.Property(x => x.EndTime).IsRequired();
            b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.HasOne(x => x.Listing).WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.ListingId).IsUnique();
            b.HasCheckConstraint("CK_Auctions_times", "EndTime > StartTime");
            b.HasCheckConstraint("CK_Auctions_prices", "StartPrice >= 0 AND (ReservePrice IS NULL OR ReservePrice >= 0)");
        });

        // Bids
        modelBuilder.Entity<Bid>(b =>
        {
            b.HasKey(x => x.BidId);
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            b.Property(x => x.PlacedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.HasOne(x => x.Auction).WithMany().HasForeignKey(x => x.AuctionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Bidder).WithMany().HasForeignKey(x => x.BidderId).OnDelete(DeleteBehavior.Restrict);
            b.HasCheckConstraint("CK_Bids_amount", "Amount > 0");
            b.HasIndex(x => new { x.AuctionId, x.PlacedAt });
            b.HasIndex(x => new { x.AuctionId, x.Amount });
        });

        // AuctionResult
        modelBuilder.Entity<AuctionResult>(b =>
        {
            b.HasKey(x => x.AuctionId);
            b.HasOne(x => x.Auction).WithMany().HasForeignKey(x => x.AuctionId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.WinningBid).WithMany().HasForeignKey(x => x.WinningBidId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.DecidedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // Payments
        modelBuilder.Entity<Payment>(b =>
        {
            b.HasKey(x => x.PaymentId);
            b.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
            b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.HasOne(x => x.Auction).WithMany().HasForeignKey(x => x.AuctionId).OnDelete(DeleteBehavior.Restrict);
            b.HasCheckConstraint("CK_Payments_amount", "Amount >= 0");
            b.HasIndex(x => new { x.AuctionId, x.CreatedAt });
        });

        // Notifications
        modelBuilder.Entity<Notification>(b =>
        {
            b.HasKey(x => x.NotificationId);
            b.Property(x => x.Title).HasMaxLength(120).IsRequired();
            b.Property(x => x.Message).HasColumnType("NVARCHAR(MAX)");
            b.Property(x => x.IsRead).HasDefaultValue(false);
            b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.HasCheckConstraint("CK_Notifications_OneLink",
                "(CASE WHEN AuctionId IS NULL THEN 0 ELSE 1 END) + (CASE WHEN ListingId IS NULL THEN 0 ELSE 1 END) + (CASE WHEN BidId IS NULL THEN 0 ELSE 1 END) + (CASE WHEN PaymentId IS NULL THEN 0 ELSE 1 END) <= 1");
            b.HasIndex(x => new { x.UserId, x.CreatedAt });
        });
    }
}