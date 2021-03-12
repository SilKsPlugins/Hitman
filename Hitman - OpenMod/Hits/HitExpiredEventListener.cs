using Hitman.Hits.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OpenMod.API.Eventing;
using OpenMod.API.Users;
using OpenMod.Core.Users;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Unturned.Users;
using System.Threading.Tasks;

namespace Hitman.Hits
{
    public class HitExpiredEventListener : IEventListener<HitExpiredEvent>
    {
        private readonly IUnturnedUserDirectory _userDirectory;
        private readonly IEconomyProvider _economyProvider;
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer _stringLocalizer;

        public HitExpiredEventListener(
            IUnturnedUserDirectory userDirectory,
            IEconomyProvider economyProvider,
            IConfiguration configuration,
            IStringLocalizer stringLocalizer)
        {
            _userDirectory = userDirectory;
            _economyProvider = economyProvider;
            _configuration = configuration;
            _stringLocalizer = stringLocalizer;
        }

        public async Task HandleEventAsync(object? sender, HitExpiredEvent @event)
        {
            if (@event.Hit.HirerPlayerId != null)
            {
                if (_configuration.GetValue("hits:refundExpired", false))
                {
                    await _economyProvider.UpdateBalanceAsync(@event.Hit.HirerPlayerId, KnownActorTypes.Player,
                        @event.Hit.Bounty, _stringLocalizer["transactions:hit_expired"]);
                }

                if (_configuration.GetValue("hits:expired:tellHirer", true))
                {
                    var hirer = _userDirectory.FindUser(@event.Hit.HirerPlayerId, UserSearchMode.FindById);

                    if (hirer != null)
                    {
                        await hirer.PrintMessageAsync(_stringLocalizer["announcements:hit_expired:hirer",
                            new {@event.Hit.Bounty, _economyProvider.CurrencySymbol, _economyProvider.CurrencyName}]);
                    }
                }
            }


            if (_configuration.GetValue("hits:expired:tellTarget", true))
            {
                var target = _userDirectory.FindUser(@event.Hit.TargetPlayerId, UserSearchMode.FindById);

                if (target != null)
                {
                    await target.PrintMessageAsync(_stringLocalizer["announcements:hit_expired:target",
                        new {@event.Hit.Bounty, _economyProvider.CurrencySymbol, _economyProvider.CurrencyName }]);
                }
            }
        }
    }
}
