namespace LogiTrack.Data;

using Microsoft.EntityFrameworkCore;
using LogiTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

public class LogiTrackContext : IdentityDbContext<ApplicationUser>
{
  public DbSet<InventoryItem> InventoryItems { get; set; }
  public DbSet<Order> Orders { get; set; }
  public DbSet<OrderItem> OrderItems { get; set; }

  public LogiTrackContext(DbContextOptions<LogiTrackContext> options) : base(options)
  {
  }

  protected override void OnConfiguring(DbContextOptionsBuilder options)
  => options.UseSqlite("Data Source=logitrack.db");

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<OrderItem>()
      .HasOne(oi => oi.Order)
      .WithMany(o => o.OrderItems)
      .HasForeignKey(oi => oi.OrderId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}