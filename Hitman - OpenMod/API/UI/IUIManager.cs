using OpenMod.API.Ioc;
using OpenMod.Unturned.Users;
using System.Threading.Tasks;

namespace Hitman.API.UI
{
    [Service]
    public interface IUIManager
    {
        Task Toggle(UnturnedUser user);
    }
}
