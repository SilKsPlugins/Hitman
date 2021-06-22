using Autofac;
using Cysharp.Threading.Tasks;
using Hitman.API.Hits;
using Hitman.API.UI;
using Hitman.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenMod.API.Permissions;
using OpenMod.API.Plugins;
using OpenMod.Core.Permissions;
using OpenMod.Unturned.Plugins;
using System;

[assembly: PluginMetadata("Hitman", DisplayName = "Hitman")]

[assembly: RegisterPermission("hitman",
    DefaultGrant = PermissionGrantResult.Deny,
    Description = "Grants the ability to kill players to claim bounties.")]

[assembly: RegisterPermission("ui",
    DefaultGrant = PermissionGrantResult.Deny,
    Description = "Grants the ability to see the hitman UI")]

[assembly: RegisterPermission("exempt",
    DefaultGrant = PermissionGrantResult.Deny,
    Description = "Disables placing hits on this player.")]

namespace Hitman
{
    public class HitmanPlugin : OpenModUnturnedPlugin
    {
        private readonly HitsDbContext _dbContext;
        private readonly ILogger<HitmanPlugin> _logger;

        public HitmanPlugin(HitsDbContext dbContext,
            ILogger<HitmanPlugin> logger,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        protected override async UniTask OnLoadAsync()
        {
            _logger.LogInformation($"{DisplayName} by SilK is loading...");
            _logger.LogInformation($"For support, join my plugin discord: https://discord.gg/zWD6fg2r5b");

            try
            {
                await _dbContext.Database.MigrateAsync();
            }
            catch (ArgumentNullException ex)
            {
                if (ex.ParamName == "connectionString")
                {
                    throw new Exception("No connection string is specified in the configuration.");
                }
            }
            catch (InvalidOperationException ex) when (ex.InnerException?.Message == "Unable to connect to any of the specified MySQL hosts.")
            {
                throw new Exception("The configured connection string is incorrect.");
            }
            await _dbContext.Database.MigrateAsync();

            LifetimeScope.Resolve<IHitExpirer>();
            LifetimeScope.Resolve<IHitsUIManager>();
            LifetimeScope.Resolve<IHitCompleter>();
        }
    }
}
