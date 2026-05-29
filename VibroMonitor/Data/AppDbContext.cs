using Microsoft.EntityFrameworkCore;
using VibroMonitor.Models;

namespace VibroMonitor.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<EquipmentItem> EquipmentItems { get; set; } = null!;
    public DbSet<EquipmentPoint> EquipmentPoints { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EquipmentItem>()
            .HasMany(e => e.Points)
            .WithOne(p => p.EquipmentItem)
            .HasForeignKey(p => p.EquipmentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // PointHistory is handled by server; client does not store history.
    }
}
