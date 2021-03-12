using OpenMod.API.Ioc;
using Steamworks;
using System.Threading.Tasks;

namespace Hitman.API.Steam
{
    [Service]
    public interface ISteamProfileFetcher
    {
        Task<ISteamProfile?> GetSteamProfile(CSteamID steamId);
    }
}
