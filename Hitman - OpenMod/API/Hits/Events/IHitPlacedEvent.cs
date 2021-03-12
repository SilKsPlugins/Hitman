using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hitman.API.Hits.Events
{
    public interface IHitPlacedEvent : IHitUpdatedEvent
    {
        IHitData Hit { get; }
    }
}
