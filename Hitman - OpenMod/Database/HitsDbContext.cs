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
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HitData>()
                .HasKey(x => x.HitId);
        }

        protected override string TablePrefix => "";
    }
}
