using Hitman.API.Hits;
using Hitman.Database;
using Hitman.Database.Models;
using Hitman.Hits.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.Core.Permissions;
using SilK.Unturned.Extras.Dispatcher;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;

namespace Hitman.Hits
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Transient, Priority = Priority.Lowest)]
    public class HitManager : IHitManager
    {
        private readonly HitmanPlugin _plugin;
        private readonly IEventBus _eventBus;
        private readonly IActionDispatcher _dispatcher;
        private ILogger<HitManager> _logger;

        public HitManager(
            HitmanPlugin plugin,
            IEventBus eventBus,
            IActionDispatcher dispatcher,
            ILogger<HitManager> logger)
        {
            _plugin = plugin;
            _eventBus = eventBus;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public Task<IEnumerable<ICombinedHitData>> GetCombinedHitsData()
        {
            return _dispatcher.Enqueue(async () =>
            {
                await using var dbContext = _plugin.LifetimeScope.Resolve<HitsDbContext>();

                return (await dbContext.Hits.ToListAsync())
                    .GroupBy(x => x.TargetPlayerId)
                    .Select(x => (ICombinedHitData) CombinedHitData.GetCombinedHitData(x.Key, x));
            });
        }

        public Task<ICombinedHitData?> GetCombinedHitData(CSteamID steamId)
        {
            return _dispatcher.Enqueue(async () =>
            {
                await using var dbContext = _plugin.LifetimeScope.Resolve<HitsDbContext>();

                var strId = steamId.ToString();

                var hits = await dbContext.Hits.Where(x => x.TargetPlayerId == strId).ToListAsync();

                return hits.Count == 0 ? null : (ICombinedHitData)CombinedHitData.GetCombinedHitData(strId, hits);
            });
        }

        public Task PlaceHit(string playerId, decimal bounty, string? hirerId = null)
        {
            return _dispatcher.Enqueue(async () =>
            {
                await using var dbContext = _plugin.LifetimeScope.Resolve<HitsDbContext>();

                var hitData = new HitData()
                {
                    TargetPlayerId = playerId,
                    HirerPlayerId = hirerId,
                    Bounty = bounty,
                    TimePlaced = DateTime.UtcNow
                };

                await dbContext.Hits.AddAsync(hitData);

                await dbContext.SaveChangesAsync();

                var @event = new HitPlacedEvent(hitData);

                await _eventBus.EmitAsync(_plugin, this, @event);
            });
        }
        
        public Task RemoveHit(IHitData hit)
        {
            return _dispatcher.Enqueue(async () =>
            {
                await using var dbContext = _plugin.LifetimeScope.Resolve<HitsDbContext>();

                if (hit is not HitData hitData) return;

                dbContext.Hits.Remove(hitData);

                await dbContext.SaveChangesAsync();
            });
        }

        public Task RemoveHits(string playerId)
        {
            return _dispatcher.Enqueue(async () =>
            {
                await using var dbContext = _plugin.LifetimeScope.Resolve<HitsDbContext>();

                var hits = await dbContext.Hits.Where(x => x.TargetPlayerId == playerId).ToListAsync();

                dbContext.Hits.RemoveRange(hits);

                await dbContext.SaveChangesAsync();
            });
        }

        public Task ClearExpiredHits(TimeSpan duration)
        {
            return _dispatcher.Enqueue(async () =>
            {
                await using var dbContext = _plugin.LifetimeScope.Resolve<HitsDbContext>();

                var expireTime = DateTime.UtcNow.Subtract(duration);

                var expiredHits = await dbContext.Hits.Where(x => x.TimePlaced < expireTime).ToListAsync();

                dbContext.Hits.RemoveRange(expiredHits);

                await dbContext.SaveChangesAsync();

                foreach (var @event in expiredHits.Select(hit => new HitExpiredEvent(hit)))
                {
                    _logger.LogInformation(
                        $"Expiring hit on player {@event.Hit.TargetPlayerId} placed by {@event.Hit.HirerPlayerId ?? "Console"} for {@event.Hit.Bounty}");

                    await _eventBus.EmitAsync(_plugin, this, @event);
                }
            });
        }
    }
}
