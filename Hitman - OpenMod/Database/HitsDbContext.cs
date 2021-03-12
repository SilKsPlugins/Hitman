using Hitman.Database.Models;
using Microsoft.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore;
using System;

namespace Hitman.Database
{
    public class HitsDbContext : OpenModDbContext<HitsDbContext>
    {
        public HitsDbContext(DbContextOptions<HitsDbContext> options, IServiceProvider serviceProvider)
            : base(options, serviceProvider)
        {
        }

        public DbSet<HitData> Hits { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HitData>()
                .HasKey(x => x.HitId);
        }
    }
}
