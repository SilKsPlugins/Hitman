using Autofac;
using Cysharp.Threading.Tasks;
using Hitman.UI;
using OpenMod.API;
using OpenMod.API.Localization;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using SilK.Unturned.Extras.UI;
using System;

namespace Hitman.Commands
{
    [Command("hitsui", Priority = Priority.High)]
    [CommandDescription("Toggles the Hitman UI.")]
    [CommandActor(typeof(UnturnedUser))]
    public class CHitsUI : UnturnedCommand
    {
        private readonly IUIManager _uiManager;
        private readonly IOpenModComponent _component;
        private readonly IPermissionChecker _permissionChecker;

        public CHitsUI(
            IUIManager uiManager,
            IOpenModComponent component,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _uiManager = uiManager;
            _component = component;
            _permissionChecker = _component.LifetimeScope.Resolve<IPermissionChecker>();
        }

        protected override async UniTask OnExecuteAsync()
        {
            var user = (UnturnedUser) Context.Actor;

            var session = await _uiManager.GetSession<HitsUISession>(user);

            if (session == null)
            {

                if (await _permissionChecker.CheckPermissionAsync(user, "ui") != PermissionGrantResult.Grant)
                    throw new NotEnoughPermissionException(_component.LifetimeScope.Resolve<IOpenModStringLocalizer>(),
                        "Hitman:ui");

                await _uiManager.StartSession<HitsUISession>(user, lifetimeScope: _component.LifetimeScope);
            }
            else
            {
                await session.EndAsync();
            }
        }
    }
}
