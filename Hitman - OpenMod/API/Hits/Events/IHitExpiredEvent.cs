namespace Hitman.API.Hits.Events
{
    public interface IHitExpiredEvent : IHitUpdatedEvent
    {
        IHitData Hit { get; }
    }
}
