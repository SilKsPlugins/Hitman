using Hitman.API.Hits;
using Hitman.API.Hits.Events;

namespace Hitman.Hits.Events
{
    public class HitExpiredEvent : HitUpdatedEvent, IHitExpiredEvent
    {
        public IHitData Hit { get; }

        public HitExpiredEvent(IHitData hitData)
        {
            Hit = hitData;
        }
    }
}
