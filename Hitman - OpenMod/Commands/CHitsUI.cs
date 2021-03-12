using Cysharp.Threading.Tasks;
using Hitman.API.UI;
using OpenMod.API.Prioritization;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using System;

namespace Hitman.Commands
{
    [Command("hitsui", Priority = Priority.High)]
    [CommandDescription("Toggles the Hitman UI.")]
    [CommandActor(typeof(UnturnedUser))]
    public class CHitsUI : UnturnedCommand
    {
        private readonly IUIManager _uiManager;

        public CHitsUI(
            IUIManager uiManager,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _uiManager = uiManager;
        }

        protected override async UniTask OnExecuteAsync()
        {
            var user = (UnturnedUser) Context.Actor;

            await _uiManager.Toggle(user);
        }
    }
}
