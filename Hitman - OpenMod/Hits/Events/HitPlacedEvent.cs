using Hitman.API.Hits;
using Hitman.API.Hits.Events;

namespace Hitman.Hits.Events
{
    public class HitPlacedEvent : HitUpdatedEvent, IHitPlacedEvent
    {
        public IHitData Hit { get; }

        public HitPlacedEvent(IHitData hit)
        {
            Hit = hit;
        }
    }
}
