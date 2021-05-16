using Hitman.API.Hits;
using Hitman.API.Hits.Events;
using System.Collections.Generic;

namespace Hitman.Hits.Events
{
    public class HitCompletedEvent : HitUpdatedEvent, IHitCompletedEvent
    {
        public ICombinedHitData CombinedHit { get; }

        public HitCompletedEvent(ICombinedHitData combinedHit)
        {
            CombinedHit = combinedHit;
        }
    }
}
