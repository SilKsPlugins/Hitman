namespace Hitman.UI
{
    public class UIHitEntry
    {
        public string? Icon { get; set; }

        public string Name { get; }

        public string Bounty { get; }

        public UIHitEntry(string? icon, string name, string bounty)
        {
            Icon = icon;
            Name = name;
            Bounty = bounty;
        }
    }
}
