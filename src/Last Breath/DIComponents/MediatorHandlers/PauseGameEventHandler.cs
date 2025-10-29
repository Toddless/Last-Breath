namespace LastBreath.DIComponents.MediatorHandlers
{
    using Godot;
    using System;
    using Utilities;
    using LastBreath.Script.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Events;

    internal class PauseGameEventHandler : IEventHandler<PauseGameEvent>
    {
        private readonly IUIElementProvider _uiElementProvider;

        public PauseGameEventHandler(IUIElementProvider uIElementProvider)
        {
            _uiElementProvider = uIElementProvider;
        }

        public void Handle(PauseGameEvent evnt)
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree == null) return;
            try
            {
                tree.Paused = true;
                if (!_uiElementProvider.IsInstanceTypeExist(typeof(PauseMenu), out var exist))
                    _uiElementProvider.CreateAndShowWindowElement<PauseMenu>();
                else
                {
                    ArgumentNullException.ThrowIfNull(exist);
                    if (exist.IsVisibleInTree())
                        _uiElementProvider.HideWindowElement<PauseMenu>();
                    else
                        _uiElementProvider.ShowWindowElement<PauseMenu>();
                }
            }
            catch (Exception ex)
            {
                Tracker.TrackException("Failed to open pause menu", ex, this);
                tree.Paused = false;
            }
        }
    }
}
