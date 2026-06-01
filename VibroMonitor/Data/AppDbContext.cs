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
    public DbSet<PointHistory> PointHistory { get; set; } = null!;
    public DbSet<AlarmItem> AlarmItem { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EquipmentItem>()
            .HasMany(e => e.Points)
            .WithOne(p => p.EquipmentItem)
            .HasForeignKey(p => p.EquipmentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // PointHistory is handled by server; client does not store history.
        modelBuilder.Entity<PointHistory>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Value);
            b.Property(x => x.Time);
            b.Property(x => x.EquipmentPointId);
        });

        modelBuilder.Entity<AlarmItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Message).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.Created).IsRequired();
            b.Property(x => x.Sensor).IsRequired();
            b.Property(x => x.Acked).HasDefaultValue(false);
            b.Property(x => x.EquipmentPointId);
            b.HasOne(x => x.EquipmentPoint).WithMany().HasForeignKey(x => x.EquipmentPointId).OnDelete(DeleteBehavior.SetNull);
            b.Property(x => x.Level).HasDefaultValue(Models.AlertLevel.Normal);
        });
    }
}
