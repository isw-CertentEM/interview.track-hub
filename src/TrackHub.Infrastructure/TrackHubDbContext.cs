using Microsoft.EntityFrameworkCore;
using TrackHub.Domain;

namespace TrackHub.Infrastructure;

public class TrackHubDbContext : DbContext
{
    public TrackHubDbContext(DbContextOptions<TrackHubDbContext> options) : base(options) { }

    public DbSet<Merchant>       Merchants       => Set<Merchant>();
    public DbSet<Carrier>        Carriers        => Set<Carrier>();
    public DbSet<Shipment>       Shipments       => Set<Shipment>();
    public DbSet<TrackingEvent>  TrackingEvents  => Set<TrackingEvent>();
    public DbSet<EventBatch>     EventBatches    => Set<EventBatch>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Merchant>().ToTable("MERCHANTS").HasKey(x => x.MerchantId);
        b.Entity<Carrier>().ToTable("CARRIERS").HasKey(x => x.CarrierId);

        b.Entity<Shipment>().ToTable("SHIPMENTS").HasKey(x => x.ShipmentId);
        b.Entity<Shipment>().HasOne(x => x.Carrier).WithMany().HasForeignKey(x => x.CarrierId);
        b.Entity<Shipment>().HasMany(x => x.Events).WithOne().HasForeignKey(x => x.ShipmentId);

        b.Entity<TrackingEvent>().ToTable("TRACKING_EVENTS").HasKey(x => x.EventId);
        b.Entity<EventBatch>().ToTable("EVENT_BATCHES").HasKey(x => x.BatchId);
    }
}
