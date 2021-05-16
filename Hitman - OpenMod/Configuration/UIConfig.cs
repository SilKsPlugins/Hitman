using System;

namespace Hitman.Configuration
{
    [Serializable]
    public class UIConfig
    {
        public bool AutoDisplay { get; set; } = true;

        public int MaxHitsShown { get; set; } = 10;

        public ushort EffectId { get; set; } = 29200;
    }
}
