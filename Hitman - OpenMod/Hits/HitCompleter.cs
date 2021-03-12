using Autofac;
using Cysharp.Threading.Tasks;
using Hitman.API.Hits;
using Hitman.Hits.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Unturned.Users;
using SDG.Unturned;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hitman.Hits
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Lowest)]
    public class HitCompleter : IHitCompleter
    {
        private readonly HitmanPlugin _plugin;
        private readonly IHitManager _hitManager;
        private readonly IUnturnedUserDirectory _userDirectory;
        private readonly IEconomyProvider _economyProvider;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IEventBus _eventBus;
        private readonly IStringLocalizer _stringLocalizer;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HitCompleter> _logger;

        public HitCompleter(
            HitmanPlugin plugin,
            IHitManager hitManager,
            IUnturnedUserDirectory userDirectory,
            IEconomyProvider economyProvider,
            IEventBus eventBus,
            IStringLocalizer stringLocalizer,
            IConfiguration configuration,
            ILogger<HitCompleter> logger)
        {
            _plugin = plugin;
            _hitManager = hitManager;
            _userDirectory = userDirectory;
            _economyProvider = economyProvider;
            _permissionChecker = plugin.LifetimeScope.Resolve<IPermissionChecker>();
            _eventBus = eventBus;
            _stringLocalizer = stringLocalizer;
            _configuration = configuration;
            _logger = logger;

            _eventBus.Subscribe(_plugin, (EventCallback<UnturnedPlayerDeathEvent>) OnPlayerDeath);
        }

        public Task OnPlayerDeath(IServiceProvider serviceProvider, object? sender, UnturnedPlayerDeathEvent @event)
        {
            async UniTask PlayerDeath()
            {
                try
                {
                    await UniTask.SwitchToThreadPool();

                    var killerId = @event.Instigator;

                    if (@event.Player.SteamId == killerId || @event.DeathCause == EDeathCause.SUICIDE) return;

                    var target = _userDirectory.GetOnlineUsers().FirstOrDefault(x => x.SteamId == @event.Player.SteamId);
                    var killer = _userDirectory.GetOnlineUsers().FirstOrDefault(x => x.SteamId == killerId);

                    if (target == null || killer == null) return;

                    if (await _permissionChecker.CheckPermissionAsync(killer, "hitman") !=
                        PermissionGrantResult.Grant) return;


                    var strId = target.SteamId.ToString();
                    var hits = await _hitManager.GetHitsData().Where(x => x.TargetPlayerId == strId).ToListAsync();

                    if (hits.Count == 0) return;

                    var combinedHit = CombinedHitData.GetCombinedHitData(strId, hits);

                    await _hitManager.RemoveHits(hits);

                    await _economyProvider.UpdateBalanceAsync(killer.Id, killer.Type, combinedHit.Bounty,
                        _stringLocalizer["transactions:hit_completed", new { Target = target }]);

                    await killer.PrintMessageAsync(_stringLocalizer["announcements:hit_completed:killer",
                        new
                        {
                            Target = target,
                            combinedHit.Bounty,
                            _economyProvider.CurrencyName,
                            _economyProvider.CurrencySymbol
                        }]);

                    if (_configuration.GetValue("hits:completed:announce", true))
                    {
                        foreach (var user in _userDirectory.GetOnlineUsers())
                        {
                            if (user.Equals(target) || user.Equals(killer)) continue;

                            await user.PrintMessageAsync(_stringLocalizer["announcements:hit_completed:announce",
                                new
                                {
                                    Killer = killer,
                                    Target = target,
                                    combinedHit.Bounty,
                                    _economyProvider.CurrencyName,
                                    _economyProvider.CurrencySymbol
                                }]);
                        }
                    }

                    if (_configuration.GetValue("hits:completed:tellTarget", true))
                    {
                        await target.PrintMessageAsync(_stringLocalizer["announcements:hit_completed:target",
                            new
                            {
                                Killer = killer,
                                Target = target,
                                combinedHit.Bounty,
                                _economyProvider.CurrencyName,
                                _economyProvider.CurrencySymbol
                            }]);
                    }

                    var hitCompletedEvent = new HitCompletedEvent(combinedHit, hits);
                    await _eventBus.EmitAsync(_plugin, this, hitCompletedEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during player death event listener");
                }
            }

            PlayerDeath().Forget();

            return Task.CompletedTask;
        }
    }
}
