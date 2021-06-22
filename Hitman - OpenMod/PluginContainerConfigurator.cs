using Hitman.Database;
using OpenMod.API.Plugins;
using OpenMod.EntityFrameworkCore.MySql.Extensions;

namespace Hitman
{
    public class PluginContainerConfigurator : IPluginContainerConfigurator
    {
        public void ConfigureContainer(IPluginServiceConfigurationContext context)
        {
            context.ContainerBuilder.AddMySqlDbContext<HitsDbContext>();
        }
    }
}
