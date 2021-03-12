using Hitman.API.Database;
using Hitman.API.Hits;
using Hitman.Database;
using Hitman.Database.Models;
using Hitman.Hits.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.Core.Permissions;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: RegisterPermission("hitman",
    DefaultGrant = PermissionGrantResult.Deny,
    Description = "Grants the ability to kill players to claim bounties.")]

namespace Hitman.Hits
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Transient, Priority = Priority.Lowest)]
    public class HitManager : IHitManager
    {
        private readonly HitmanPlugin _plugin;
        private readonly HitsDbContext _dbContext;
        private readonly IEventBus _eventBus;
        private readonly IActionDispatcher _dispatcher;

        public HitManager(
            HitmanPlugin plugin,
            HitsDbContext dbContext,
            IEventBus eventBus,
            IActionDispatcher dispatcher)
        {
            _plugin = plugin;
            _dbContext = dbContext;
            _eventBus = eventBus;
            _dispatcher = dispatcher;
        }

        public IQueryable<IHitData> GetHitsData() => _dbContext.Hits.AsQueryable();

        public Task<IEnumerable<ICombinedHitData>> GetCombinedHitsData()
        {
            return _dispatcher.Enqueue(async () =>
            {
                return (await GetHitsData().ToListAsync())
                    .GroupBy(x => x.TargetPlayerId)
                    .Select(x => (ICombinedHitData) CombinedHitData.GetCombinedHitData(x.Key, x));
            });
        }

        public Task<ICombinedHitData?> GetCombinedHitData(CSteamID steamId)
        {
            return _dispatcher.Enqueue(async () =>
            {
                var strId = steamId.ToString();

                var hits = await GetHitsData().Where(x => x.TargetPlayerId.Equals(strId)).ToListAsync();

                return hits.Count == 0 ? null : (ICombinedHitData)CombinedHitData.GetCombinedHitData(strId, hits);
            });
        }

        public Task PlaceHit(string playerId, decimal bounty, string? hirerId = null)
        {
            return _dispatcher.Enqueue(async () =>
            {
                var hitData = new HitData()
                {
                    TargetPlayerId = playerId,
                    HirerPlayerId = hirerId,
                    Bounty = bounty,
                    TimePlaced = DateTime.Now
                };

                await _dbContext.Hits.AddAsync(hitData);

                await _dbContext.SaveChangesAsync();

                var @event = new HitPlacedEvent(hitData);

                await _eventBus.EmitAsync(_plugin, this, @event);
            });
        }
        
        public Task RemoveHit(IHitData hit)
        {
            return _dispatcher.Enqueue(async () =>
            {
                if (hit is not HitData hitData) return;

                _dbContext.Hits.Remove(hitData);

                await _dbContext.SaveChangesAsync();
            });
        }

        public Task RemoveHits(IEnumerable<IHitData> hits)
        {
            return _dispatcher.Enqueue(async () =>
            {
                _dbContext.Hits.RemoveRange(hits.OfType<HitData>());

                await _dbContext.SaveChangesAsync();
            });
        }
    }
}
