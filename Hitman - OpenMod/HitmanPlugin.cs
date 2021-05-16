using Autofac;
using Cysharp.Threading.Tasks;
using Hitman.API.Hits;
using Hitman.API.UI;
using Hitman.Database;
using OpenMod.API.Permissions;
using OpenMod.API.Plugins;
using OpenMod.Core.Permissions;
using OpenMod.EntityFrameworkCore.Extensions;
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

        public HitmanPlugin(HitsDbContext dbContext,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _dbContext = dbContext;
        }

        protected override async UniTask OnLoadAsync()
        {
            await _dbContext.OpenModMigrateAsync();

            LifetimeScope.Resolve<IHitExpirer>();
            LifetimeScope.Resolve<IHitsUIManager>();
            LifetimeScope.Resolve<IHitCompleter>();
        }

        protected override UniTask OnUnloadAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
