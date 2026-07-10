namespace Core.Views.UI
{
    using Services;

    /// <summary>
    /// The single popup scene used for every notification (built in the editor).
    /// Receives the notification content plus the region resolved from the message category,
    /// localizes the text itself (plain key, or template key + values) and dismisses itself
    /// after a delay (Timed lifetime).
    /// </summary>
    public interface INotificationPopup : IPopup
    {
        void SetNotification(NotificationContent content, OverlayRegion region);
    }
}
