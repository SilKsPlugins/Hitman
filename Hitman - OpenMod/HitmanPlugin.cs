using Autofac;
using Cysharp.Threading.Tasks;
using Hitman.API.Hits;
using Hitman.API.UI;
using Hitman.Database;
using OpenMod.API.Plugins;
using OpenMod.EntityFrameworkCore.Extensions;
using OpenMod.Unturned.Plugins;
using System;

[assembly: PluginMetadata("Hitman", DisplayName = "Hitman")]
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
            LifetimeScope.Resolve<IUIManager>();
            LifetimeScope.Resolve<IHitCompleter>();
        }

        protected override UniTask OnUnloadAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
