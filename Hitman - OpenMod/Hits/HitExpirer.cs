using Cysharp.Threading.Tasks;
using Hitman.API.Hits;
using Hitman.Hits.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;
using OpenMod.Core.Helpers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hitman.Hits
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Low)]
    public class HitExpirer : IHitExpirer, IDisposable
    {
        private readonly HitmanPlugin _plugin;
        private readonly IConfiguration _configuration;
        private readonly IChangeToken _changeToken;
        private readonly IHitManager _hitManager;
        private readonly IEventBus _eventBus;
        private readonly ILogger<HitExpirer> _logger;

        private bool _disposed;
        private readonly CancellationTokenSource _cancellationToken;

        public HitExpirer(
            HitmanPlugin plugin,
            IConfiguration configuration,
            IHitManager hitManager,
            IEventBus eventBus,
            ILogger<HitExpirer> logger)
        {
            _plugin = plugin;
            _configuration = configuration;
            _changeToken = configuration.GetReloadToken();
            _hitManager = hitManager;
            _eventBus = eventBus;
            _logger = logger;

            _cancellationToken = new CancellationTokenSource();
            CheckExpiredHits().Forget();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cancellationToken.Cancel();
        }

        private async UniTask CheckExpiredHits()
        {
            TimeSpan checkInterval;
            TimeSpan duration;

            void ReloadConfig()
            {
                checkInterval = TimeSpanHelper.Parse(_configuration["hits:checkInterval"]);
                duration = TimeSpanHelper.Parse(_configuration["hits:duration"]);
            }

            ReloadConfig();

            while (!_cancellationToken.IsCancellationRequested)
            {
                var expireTime = DateTime.Now.Subtract(duration);

                var expiredHits = await _hitManager.GetHitsData().Where(x => x.TimePlaced < expireTime)
                    .ToListAsync(_cancellationToken.Token);

                await _hitManager.RemoveHits(expiredHits);

                foreach (var @event in expiredHits.Select(hit => new HitExpiredEvent(hit)))
                {
                    _logger.LogInformation(
                        $"Expiring hit on player {@event.Hit.TargetPlayerId} placed by {@event.Hit.HirerPlayerId ?? "Console"} for {@event.Hit.Bounty}");

                    await _eventBus.EmitAsync(_plugin, this, @event);
                }
                
                await Task.Delay(checkInterval, _cancellationToken.Token);

                if (_changeToken.HasChanged) ReloadConfig();
            }
        }
    }
}
