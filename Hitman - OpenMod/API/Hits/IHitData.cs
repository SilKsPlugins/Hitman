using System;

namespace Hitman.API.Hits
{
    public interface IHitData
    {
        string TargetPlayerId { get; }

        string? HirerPlayerId { get; }

        decimal Bounty { get; }

        DateTime TimePlaced { get; }
    }
}
