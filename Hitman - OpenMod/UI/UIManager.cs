using Autofac;
using Cysharp.Threading.Tasks;
using Hitman.API.Hits;
using Hitman.API.Steam;
using Hitman.API.UI;
using Hitman.Hits.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.API;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Localization;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Commands;
using OpenMod.Core.Permissions;
using OpenMod.Core.Plugins.Events;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Users.Events;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: RegisterPermission("ui",
    DefaultGrant = PermissionGrantResult.Deny,
    Description = "Grants the ability to see the hitman UI")]

namespace Hitman.UI
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Lowest)]
    public class UIManager : IUIManager, IUniTaskAsyncDisposable
    {
        private readonly IHitManager _hitManager;
        private readonly ISteamProfileFetcher _profileFetcher;
        private readonly IUnturnedUserDirectory _userDirectory;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IEconomyProvider _economyProvider;
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer _stringLocalizer;
        private readonly IRuntime _runtime;

        private readonly HashSet<CSteamID> _shownPlayers;
        private readonly List<UIHitEntry> _topHits;

        public const int MaxPossibleElements = 10;

        private readonly ushort _effectId;
        private readonly short _effectKey;

        public UIManager(
            IHitManager hitManager,
            ISteamProfileFetcher profileFetcher,
            IUnturnedUserDirectory userDirectory,
            IPermissionChecker permissionChecker,
            IEconomyProvider economyProvider,
            IConfiguration configuration,
            IStringLocalizer stringLocalizer,
            HitmanPlugin plugin,
            IEventBus eventBus,
            IRuntime runtime)
        {
            _hitManager = hitManager;
            _profileFetcher = profileFetcher;
            _userDirectory = userDirectory;
            _permissionChecker = permissionChecker;
            _economyProvider = economyProvider;
            _configuration = configuration;
            _stringLocalizer = stringLocalizer;
            _runtime = runtime;

            _shownPlayers = new HashSet<CSteamID>();
            _topHits = new List<UIHitEntry>();

            _effectId = _configuration.GetValue<ushort>("ui:effectId", 29200);
            _effectKey = (short) _effectId;

            eventBus.Subscribe(plugin, (EventCallback<HitUpdatedEvent>) OnHitUpdated);
            eventBus.Subscribe(plugin, (EventCallback<UnturnedUserConnectedEvent>) OnUserConnected);
            eventBus.Subscribe(plugin, (EventCallback<UnturnedUserDisconnectedEvent>)OnUserDisconnected);
            eventBus.Subscribe(plugin, (EventCallback<PluginLoadedEvent>) OnPluginLoaded);
        }

        public async Task Toggle(UnturnedUser user)
        {
            var showUI = !_shownPlayers.Contains(user.SteamId);

            if (showUI && await _permissionChecker.CheckPermissionAsync(user, "ui") != PermissionGrantResult.Grant)
                throw new NotEnoughPermissionException(_runtime.LifetimeScope.Resolve<IOpenModStringLocalizer>(),
                    "Hitman:ui");

            await UpdatePlayer(user.SteamId, showUI);
        }

        private async Task RefreshTopHits()
        {
            _topHits.Clear();

            var combinedHits =
                (await _hitManager.GetCombinedHitsData())
                .OrderByDescending(x => x.Bounty)
                .ToList();

            var maxHits = _configuration.GetValue("ui:maxHitsShown", 10);

            maxHits = maxHits > MaxPossibleElements ? MaxPossibleElements : maxHits;
            
            foreach (var hit in combinedHits.TakeWhile(_ => _topHits.Count < maxHits))
            {
                var user = _userDirectory.FindUser(hit.TargetPlayerId, UserSearchMode.FindById);

                if (user == null) continue;

                var profile = await _profileFetcher.GetSteamProfile(user.SteamId);

                var entry = new UIHitEntry(profile?.AvatarMedium,
                    _stringLocalizer["ui:playerName",
                        new {User = user, hit.Bounty, _economyProvider.CurrencyName, _economyProvider.CurrencySymbol}],
                    _stringLocalizer["ui:bounty",
                        new {User = user, hit.Bounty, _economyProvider.CurrencyName, _economyProvider.CurrencySymbol}]);

                _topHits.Add(entry);
            }
        }

        private async UniTask UpdatePlayer(CSteamID steamId, bool showUI)
        {
            void SendEffect(string arg) => EffectManager.sendUIEffect(_effectId, _effectKey, steamId, true, arg);

            void ClearEffect() => EffectManager.askEffectClearByID(_effectId, steamId);

            void SendVisibility(string key, bool visible) =>
                EffectManager.sendUIEffectVisibility(_effectKey, steamId, true, key, visible);

            void SendText(string key, string text) =>
                EffectManager.sendUIEffectText(_effectKey, steamId, true, key, text);

            void SendImage(string key, string url) =>
                EffectManager.sendUIEffectImageURL(_effectKey, steamId, true, key, url, true, true);

            await UniTask.SwitchToMainThread();

            if (showUI)
            {
                if (!_shownPlayers.Contains(steamId))
                {
                    _shownPlayers.Add(steamId);

                    ClearEffect();
                    SendEffect(_stringLocalizer["ui:header"]);
                }

                var i = 0;

                for (; i < _topHits.Count; i++)
                {
                    var entry = _topHits[i];

                    SendVisibility($"Hit ({i})", true);
                    SendText($"HitName ({i})", entry.Name);
                    SendText($"HitPrice ({i})", entry.Bounty);
                    if (entry.Icon != null) SendImage($"HitIcon ({i})", entry.Icon);
                }

                for (; i < MaxPossibleElements; i++)
                {
                    SendVisibility($"Hit ({i})", false);
                }
            }
            else
            {
                SendVisibility("CloseTrigger", false);

                async UniTask DelayClearEffect()
                {
                    await UniTask.Delay(2000);

                    await UniTask.SwitchToMainThread();

                    if (!_shownPlayers.Contains(steamId))
                        ClearEffect();
                }

                _shownPlayers.Remove(steamId);

                if (_disposed)
                    ClearEffect();
                else
                    DelayClearEffect().Forget();
            }
        }

        private async UniTask UpdateAllPlayers(bool showUI = true)
        {
            await UniTask.SwitchToMainThread();

            foreach (var user in _userDirectory.GetOnlineUsers().Where(x => _shownPlayers.Contains(x.SteamId)))
            {
                await UpdatePlayer(user.SteamId, showUI);
            }
        }

        private async UniTask CheckShouldRefreshHits(UnturnedUser user)
        {
            await UniTask.SwitchToThreadPool();

            var hitInfo = await _hitManager.GetCombinedHitData(user.SteamId);

            if (hitInfo == null) return;

            await RefreshTopHits();

            await UpdateAllPlayers();
        }

        private Task OnHitUpdated(IServiceProvider serviceProvider, object? sender, HitUpdatedEvent @event)
        {
            async UniTask HitUpdated()
            {
                await UniTask.SwitchToThreadPool();

                await RefreshTopHits();

                await UpdateAllPlayers();
            }

            HitUpdated().Forget();

            return Task.CompletedTask;
        }

        private Task OnUserConnected(IServiceProvider serviceProvider, object? sender, UnturnedUserConnectedEvent @event)
        {
            CheckShouldRefreshHits(@event.User).Forget();

            async UniTask UserConnected()
            {
                if (await _permissionChecker.CheckPermissionAsync(@event.User, "ui") ==
                    PermissionGrantResult.Grant)
                {
                    await UpdatePlayer(@event.User.SteamId, true);
                }
            }

            return UserConnected().AsTask();
        }

        private Task OnUserDisconnected(IServiceProvider serviceProvider, object? sender, UnturnedUserDisconnectedEvent @event)
        {
            _shownPlayers.Remove(@event.User.SteamId);

            CheckShouldRefreshHits(@event.User).Forget();

            return Task.CompletedTask;
        }

        private Task OnPluginLoaded(IServiceProvider serviceProvider, object? sender, PluginLoadedEvent @event)
        {
            if (@event.Plugin.GetType() != typeof(HitmanPlugin)) return Task.CompletedTask;

            async UniTask Startup()
            {
                await RefreshTopHits();

                await UniTask.SwitchToMainThread();
                
                if (_configuration.GetValue("ui:autoDisplay", true))
                {
                    foreach (var user in _userDirectory.GetOnlineUsers())
                    {
                        if (await _permissionChecker.CheckPermissionAsync(user, "ui") != PermissionGrantResult.Grant)
                            continue;

                        await UpdatePlayer(user.SteamId, true);
                    }
                }
            }
            Startup().Forget();

            return Task.CompletedTask;
        }

        private bool _disposed;

        public async UniTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await UpdateAllPlayers(false);

            _shownPlayers.Clear();
        }
    }
}
