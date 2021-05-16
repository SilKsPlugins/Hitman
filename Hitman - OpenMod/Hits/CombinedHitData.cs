using Hitman.API.Hits;
using System.Collections.Generic;
using System.Linq;

namespace Hitman.Hits
{
    public class CombinedHitData : ICombinedHitData
    {
        public string TargetPlayerId { get; }
        
        public decimal Bounty { get; set; }

        private readonly List<IHitData> _individualHits;

        public IReadOnlyCollection<IHitData> IndividualHits => _individualHits.AsReadOnly();

        private CombinedHitData(string targetPlayerId)
        {
            TargetPlayerId = targetPlayerId;
            Bounty = 0;

            _individualHits = new List<IHitData>();
        }

        public static CombinedHitData GetCombinedHitData(string targetPlayerId, IEnumerable<IHitData> hitsData)
        {
            var combined = new CombinedHitData(targetPlayerId);

            var hitsDataList = hitsData.ToList();

            combined._individualHits.AddRange(hitsDataList);

            foreach (var hitData in hitsDataList.Where(x => x.TargetPlayerId.Equals(targetPlayerId)))
            {
                combined.Bounty += hitData.Bounty;
            }

            return combined;
        }
    }
}
