using Hitman.API.Hits;
using System;

namespace Hitman.Database.Models
{
    public class HitData : IHitData
    {
        public int HitId { get; set; }

        public string TargetPlayerId { get; set; }

        public string HirerPlayerId { get; set; }

        public decimal Bounty { get; set; }

        public DateTime TimePlaced { get; set; }

        public HitData()
        {
            HitId = 0;
            TargetPlayerId = "";
            HirerPlayerId = null!;
            Bounty = 0;
            TimePlaced = default;
        }
    }
}
