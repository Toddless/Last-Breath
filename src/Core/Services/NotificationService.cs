namespace Core.Services
{
    using System;
    using System.Threading.Tasks;
    using Events;
    using Godot;
    using Views.UI;

    /// <summary>
    /// The single consumer of <see cref="SendNotificationMessageMessage"/>: creates a fresh popup
    /// via the registered factory and shows it on the notification layer. The project bootstrap
    /// must call <see cref="Setup"/> once the layer manager and the popup scene exist;
    /// until then messages are silently dropped.
    /// </summary>
    public class NotificationService : IMessageHandler<SendNotificationMessageMessage>
    {
        private ILayerManager? _layers;
        private Func<Control?>? _popupFactory;

        /// <summary>
        /// The factory returns the popup Control implementing <see cref="INotificationPopup"/>.
        /// It may return null (e.g. the popup scene isn't built yet) — the message is then dropped.
        /// </summary>
        public void Setup(ILayerManager layers, Func<Control?> popupFactory)
        {
            _layers = layers;
            _popupFactory = popupFactory;
        }

        public Task HandleMessageAsync(SendNotificationMessageMessage message)
        {
            if (_layers == null || _popupFactory == null) return Task.CompletedTask;

            var popup = _popupFactory();
            if (popup == null) return Task.CompletedTask;

            if (popup is INotificationPopup notification)
                notification.SetNotification(message.Id);

            _layers.ShowNotification(popup);
            return Task.CompletedTask;
        }
    }
}
