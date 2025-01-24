using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace App.Models;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Edit table Identity name
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (!string.IsNullOrEmpty(tableName) && tableName.StartsWith("AspNet"))
            {
                entityType.SetTableName(tableName.Substring(6));
            }
        }

        // Users
        modelBuilder.Entity<AppUser>()
            .Property(u => u.BirthDate)
            .HasColumnType("DATE");

        // LoggedBrowsers
        modelBuilder.Entity<LoggedBrowsersModel>()
            .HasOne(l => l.User)
            .WithMany(u => u.LoggedBrowsers)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Memberships
        modelBuilder.Entity<MembershipsModel>()
            .HasIndex(m => m.UserId)
            .IsUnique();
        modelBuilder.Entity<MembershipsModel>()
            .HasOne(m => m.User)
            .WithOne(u => u.Membership)
            .OnDelete(DeleteBehavior.Cascade);

        // EditedImages
        modelBuilder.Entity<EditedImagesModel>()
            .HasOne(e => e.User)
            .WithMany(u => u.EditedImages)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public DbSet<LoggedBrowsersModel> LoggedBrowsers { get; set; }
    public DbSet<MembershipsModel> Memberships { get; set; }
    public DbSet<EditedImagesModel> EditedImages { get; set; }
}