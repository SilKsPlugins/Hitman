using System;
using OpenMod.API.Ioc;
using Steamworks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hitman.API.Hits
{
    [Service]
    public interface IHitManager
    {
        Task<IEnumerable<ICombinedHitData>> GetCombinedHitsData();

        Task<ICombinedHitData?> GetCombinedHitData(CSteamID steamId);

        Task PlaceHit(string playerId, decimal bounty, string? hirerId = null);

        Task RemoveHit(IHitData hit);

        Task RemoveHits(string playerId);

        Task ClearExpiredHits(TimeSpan duration);
    }
}
