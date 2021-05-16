using Cysharp.Threading.Tasks;
using Hitman.API.Hits;
using Hitman.API.Steam;
using Hitman.API.UI;
using Hitman.Hits.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.API;
using OpenMod.API.Eventing;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.API.Users;
using OpenMod.Core.Plugins.Events;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Users;
using OpenMod.Unturned.Users.Events;
using SilK.Unturned.Extras.Events;
using SilK.Unturned.Extras.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hitman.UI
{
    [PluginServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Lowest)]
    public class HitsUIManager : IHitsUIManager,
        IInstanceAsyncEventListener<UnturnedUserConnectedEvent>,
        IInstanceAsyncEventListener<UnturnedUserDisconnectedEvent>,
        IInstanceAsyncEventListener<HitUpdatedEvent>,
        IInstanceAsyncEventListener<PluginLoadedEvent>
    {
        private readonly IHitManager _hitManager;
        private readonly IUIManager _uiManager;
        private readonly ISteamProfileFetcher _profileFetcher;
        private readonly IUnturnedUserDirectory _userDirectory;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IEconomyProvider _economyProvider;
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer _stringLocalizer;
        private readonly IOpenModComponent _component;
        private readonly IEventBus _eventBus;
        private readonly ILogger<HitsUIManager> _logger;
        
        private readonly List<UIHitEntry> _topHits;

        public const int MaxPossibleElements = 10;

        public HitsUIManager(
            IHitManager hitManager,
            IUIManager uiManager,
            ISteamProfileFetcher profileFetcher,
            IUnturnedUserDirectory userDirectory,
            IPermissionChecker permissionChecker,
            IEconomyProvider economyProvider,
            IConfiguration configuration,
            IStringLocalizer stringLocalizer,
            IOpenModComponent component,
            IEventBus eventBus,
            ILogger<HitsUIManager> logger)
        {
            _hitManager = hitManager;
            _uiManager = uiManager;
            _profileFetcher = profileFetcher;
            _userDirectory = userDirectory;
            _permissionChecker = permissionChecker;
            _economyProvider = economyProvider;
            _configuration = configuration;
            _stringLocalizer = stringLocalizer;
            _component = component;
            _eventBus = eventBus;
            _logger = logger;

            _topHits = new List<UIHitEntry>();
        }

        public IReadOnlyCollection<UIHitEntry> GetTopHits() => _topHits.AsReadOnly();

        private async Task RefreshTopHits()
        {
            _logger.LogDebug("Refreshing top hits...");

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

            _logger.LogDebug("Total hits to show: " + _topHits.Count);

            await _eventBus.EmitAsync(_component, this, new HitListUpdatedEvent());
        }
        

        private async UniTask CheckShouldRefreshHits(UnturnedUser user)
        {
            await UniTask.SwitchToThreadPool();

            var hitInfo = await _hitManager.GetCombinedHitData(user.SteamId);

            if (hitInfo == null) return;

            await RefreshTopHits();
        }

        private async UniTask<bool> TryStartUISession(UnturnedUser user)
        {
            if (await _permissionChecker.CheckPermissionAsync(user, "ui") == PermissionGrantResult.Grant)
            {
                await _uiManager.StartSession<HitsUISession>(user, lifetimeScope: _component.LifetimeScope);

                return true;
            }

            return false;
        }

        public async UniTask HandleEventAsync(object? sender, UnturnedUserConnectedEvent @event)
        {
            await CheckShouldRefreshHits(@event.User);

            await TryStartUISession(@event.User);
        }

        public async UniTask HandleEventAsync(object? sender, UnturnedUserDisconnectedEvent @event)
        {
            await CheckShouldRefreshHits(@event.User);
        }

        public async UniTask HandleEventAsync(object? sender, HitUpdatedEvent @event)
        {
            await UniTask.SwitchToThreadPool();

            await RefreshTopHits();
        }

        public async UniTask HandleEventAsync(object? sender, PluginLoadedEvent @event)
        {
            if (@event.Plugin.GetType() != typeof(HitmanPlugin)) return;

            await RefreshTopHits();

            await UniTask.SwitchToMainThread();

            if (_configuration.GetValue("ui:autoDisplay", true))
            {
                foreach (var user in _userDirectory.GetOnlineUsers())
                {
                    await TryStartUISession(user);
                }
            }
        }
    }
}
