using Cysharp.Threading.Tasks;
using Hitman.API.Hits;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.Core.Commands;
using OpenMod.Core.Console;
using OpenMod.Core.Permissions;
using OpenMod.Core.Rcon;
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using System;

[assembly: RegisterPermission("exempt",
    DefaultGrant = PermissionGrantResult.Deny,
    Description = "Disables placing hits on this player.")]

namespace Hitman.Commands
{
    [Command("hit", Priority = Priority.High)]
    [CommandAlias("bounty")]
    [CommandSyntax("<player> <bounty>")]
    [CommandDescription("Place a bounty on a player.")]
    [CommandActor(typeof(UnturnedUser))]
    [CommandActor(typeof(ConsoleActor))]
    [CommandActor(typeof(IRconClient))]
    public class CHit : UnturnedCommand
    {
        private readonly IHitManager _hitManager;
        private readonly IEconomyProvider _economyProvider;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IUnturnedUserDirectory _userDirectory;
        private readonly IStringLocalizer _stringLocalizer;
        private readonly IConfiguration _configuration;

        public CHit(IHitManager hitManager,
            IEconomyProvider economyProvider,
            IPermissionChecker permissionChecker,
            IUnturnedUserDirectory userDirectory,
            IStringLocalizer stringLocalizer,
            IConfiguration configuration,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _hitManager = hitManager;
            _economyProvider = economyProvider;
            _permissionChecker = permissionChecker;
            _userDirectory = userDirectory;
            _stringLocalizer = stringLocalizer;
            _configuration = configuration;
        }

        protected override async UniTask OnExecuteAsync()
        {
            await UniTask.SwitchToThreadPool();

            var target
                = await Context.Parameters.GetAsync<UnturnedUser>(0);
            var bounty = await Context.Parameters.GetAsync<decimal>(1);

            var minimumBounty = _configuration.GetValue<decimal>("hits:minimumBounty", 100);

            if (bounty < minimumBounty)
                throw new UserFriendlyException(_stringLocalizer["commands:errors:invalid_bounty",
                    new {MinimumBounty = minimumBounty, Bounty = bounty}]);

            var hirer = Context.Actor;
            var unturnedHirer = hirer as UnturnedUser;

            if (unturnedHirer != null && target.Equals(unturnedHirer) &&
                !_configuration.GetValue("hits:canPlaceOnSelf", false))
                throw new UserFriendlyException(_stringLocalizer["commands:errors:cannot_self_place"]);

            if (await _permissionChecker.CheckPermissionAsync(target, "exempt") == PermissionGrantResult.Grant)
                throw new UserFriendlyException(_stringLocalizer["commands:errors:target_hit_exempt",
                    new {Target = target}]);

            if (Context.Actor.Type == KnownActorTypes.Player)
                await _economyProvider.UpdateBalanceAsync(Context.Actor.Id, Context.Actor.Type, -bounty,
                    _stringLocalizer["transactions:hit_placed", new {Hirer = hirer, Target = target, Bounty = bounty}]);

            await _hitManager.PlaceHit(target.Id, bounty,
                Context.Actor.Type == KnownActorTypes.Player ? Context.Actor.Id : null);

            await PrintAsync(_stringLocalizer["commands:success:hit_placed:hirer",
                new
                {
                    Target = target, Bounty = bounty, _economyProvider.CurrencyName, _economyProvider.CurrencySymbol
                }]);

            if (_configuration.GetValue("hits:placed:announce", true))
            {
                foreach (var user in _userDirectory.GetOnlineUsers())
                {
                    if (unturnedHirer != null && unturnedHirer.Equals(user)) continue;

                    if (target.Equals(user)) continue;

                    await user.PrintMessageAsync(_stringLocalizer["commands:success:hit_placed:announce",
                        new
                        {
                            Hirer = hirer, Target = target, Bounty = bounty, _economyProvider.CurrencyName,
                            _economyProvider.CurrencySymbol
                        }]);
                }
            }

            if (_configuration.GetValue("hits:placed:tellTarget", true))
            {
                await target.PrintMessageAsync(_stringLocalizer["commands:success:hit_placed:target",
                    new
                    {
                        Hirer = hirer, Target = target, Bounty = bounty, _economyProvider.CurrencyName,
                        _economyProvider.CurrencySymbol
                    }]);
            }
        }
    }
}
