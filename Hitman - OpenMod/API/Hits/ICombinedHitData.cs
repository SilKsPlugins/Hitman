namespace Hitman.API.Hits
{
    public interface ICombinedHitData
    {
        string TargetPlayerId { get; }
        
        decimal Bounty { get; }
    }
}
