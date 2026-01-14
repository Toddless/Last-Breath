namespace LastBreath.DIComponents.MediatorHandlers
{
    using Godot;
    using System;
    using Utilities;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;

    public class OpenWindowEventHandler<TEvent, TWindow> : IEventHandler<TEvent>
        where TEvent : IEvent
        where TWindow : Control, IInitializable, IRequireServices, IClosable
    {
        private readonly IUIElementProvider _uiElementProvider;

        public OpenWindowEventHandler(IUIElementProvider uiElementProvider)
        {
            _uiElementProvider = uiElementProvider;
        }

        public void Handle(TEvent evnt)
        {
            try
            {
                if (!_uiElementProvider.IsInstanceTypeExist(typeof(TWindow), out Control? exist))
                    _uiElementProvider.CreateAndShowWindowElement<TWindow>();
                else
                {
                    ArgumentNullException.ThrowIfNull(exist);
                    if (exist.IsVisibleInTree())
                        _uiElementProvider.HideWindowElement<TWindow>();
                    else
                        _uiElementProvider.ShowWindowElement<TWindow>();
                }
            }
            catch (Exception ex)
            {
                Tracker.TrackException($"Failed to open window: {typeof(TWindow)}", ex, this);
            }
        }
    }
}
