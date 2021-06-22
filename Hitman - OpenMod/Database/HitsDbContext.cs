using Hitman.Database.Models;
using Microsoft.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore.Configurator;
using System;

namespace Hitman.Database
{
    public class HitsDbContext : OpenModDbContext<HitsDbContext>
    {
        public HitsDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public HitsDbContext(IDbContextConfigurator configurator, IServiceProvider serviceProvider) : base(configurator,
            serviceProvider)
        {
        }

        public DbSet<HitData> Hits => Set<HitData>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HitData>(entity =>
            {
                entity.HasKey(e => e.HitId)
                    .HasName("PRIMARY");

                entity.Property(e => e.Bounty).HasColumnType("decimal(18,2)");

                entity.Property(e => e.HirerPlayerId)
                    .HasColumnType("text")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_0900_ai_ci");

                entity.Property(e => e.TargetPlayerId)
                    .IsRequired()
                    .HasColumnType("text")
                    .HasCharSet("utf8mb4")
                    .HasCollation("utf8mb4_0900_ai_ci");

                entity.Property(e => e.TimePlaced).HasColumnType("datetime");
            });
        }

        protected override string TablePrefix => "";
    }
}
