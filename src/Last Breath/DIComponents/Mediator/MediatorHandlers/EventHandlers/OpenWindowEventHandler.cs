namespace LastBreath.DIComponents.Mediator.MediatorHandlers.EventHandlers
{
    using Godot;
    using Utilities;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;

    public class OpenWindowEventHandler<TEvent, TWindow> : IEventHandler<TEvent>
        where TEvent : IEvent
        where TWindow : Control, IInitializable, IRequireServices
    {
        private readonly IUIElementProvider _uiElementProvider;

        public OpenWindowEventHandler(IUIElementProvider uiElementProvider)
        {
            _uiElementProvider = uiElementProvider;
        }

        public void Handle(TEvent evnt)
        {
            if (!_uiElementProvider.IsInstanceTypeExist(typeof(TWindow), out var exist))
                _uiElementProvider.CreateAndShowWindowElement<TWindow>();
            else
            {
                if (exist == null)
                {
                    Tracker.TrackNull($"UIElement provider has returned null instead existing instance: {typeof(TWindow)}", this);
                    return;
                }
                // not sure about it.
                if (exist.Visible)
                    exist.Hide();
                else
                    exist.Show();
            }
        }
    }
}
