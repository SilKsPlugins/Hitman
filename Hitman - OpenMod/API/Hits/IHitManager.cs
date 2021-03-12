using OpenMod.API.Ioc;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hitman.API.Hits
{
    [Service]
    public interface IHitManager
    {
        IQueryable<IHitData> GetHitsData();

        Task<IEnumerable<ICombinedHitData>> GetCombinedHitsData();

        Task<ICombinedHitData?> GetCombinedHitData(CSteamID steamId);

        Task PlaceHit(string playerId, decimal bounty, string? hirerId = null);

        Task RemoveHit(IHitData hit);

        Task RemoveHits(IEnumerable<IHitData> hits);
    }
}
