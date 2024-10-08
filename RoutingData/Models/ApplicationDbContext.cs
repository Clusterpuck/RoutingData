﻿using Microsoft.EntityFrameworkCore;

namespace RoutingData.Models;
public class ApplicationDbContext : DbContext
{
   
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
       : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<DeliveryRoute> DeliveryRoutes { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderProduct> OrderProducts { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<Calculation> Calculations { get; set; } = null!;

    // Overriding OnModelCreating to set up unique constraints and configurations
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);  // Ensures the base configurations are called

        // Adding a unique constraint on Longitude, Latitude, and CustomerName in Locations
        modelBuilder.Entity<Location>()
            .HasIndex(l => new { l.Longitude, l.Latitude, l.CustomerName })
            .IsUnique();  // Enforce uniqueness on the composite key

    }

}
