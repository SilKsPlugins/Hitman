using OpenMod.API.Ioc;
using System.Collections.Generic;

namespace Hitman.API.UI
{
    [Service]
    public interface IHitsUIManager
    {
        IReadOnlyCollection<UIHitEntry> GetTopHits();
    }
}
