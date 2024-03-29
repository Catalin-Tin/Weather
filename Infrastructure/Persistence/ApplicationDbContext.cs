﻿using Microsoft.EntityFrameworkCore;
using Weather.Models;

namespace Weather.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<City> Cities { get; set; }
        public DbSet<WeatherTable> Weather { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<City>().HasKey(c => c.Id);
            modelBuilder.Entity<WeatherTable>().HasKey(c => c.Id);

       
        
            base.OnModelCreating(modelBuilder);
        }
    }
}
