using Hitman.API.Hits;
using System.Collections.Generic;
using System.Linq;

namespace Hitman.Hits
{
    public class CombinedHitData : ICombinedHitData
    {
        public string TargetPlayerId { get; }
        
        public decimal Bounty { get; set; }

        private CombinedHitData(string targetPlayerId)
        {
            TargetPlayerId = targetPlayerId;
            Bounty = 0;
        }

        public static CombinedHitData GetCombinedHitData(string targetPlayerId, IEnumerable<IHitData> hitsData)
        {
            var combined = new CombinedHitData(targetPlayerId);

            foreach (var hitData in hitsData.Where(x => x.TargetPlayerId.Equals(targetPlayerId)))
            {
                combined.Bounty += hitData.Bounty;
            }

            return combined;
        }
    }
}
