namespace Hitman.API.Hits.Events
{
    public interface IHitCompletedEvent : IHitUpdatedEvent
    {
        ICombinedHitData CombinedHit { get; }
    }
}
