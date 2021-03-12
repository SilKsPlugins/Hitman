using System.Collections.Generic;

namespace Hitman.API.Hits.Events
{
    public interface IHitCompletedEvent : IHitUpdatedEvent
    {
        ICombinedHitData CombinedHit { get; }

        IReadOnlyCollection<IHitData> IndividualHits { get; }
    }
}
