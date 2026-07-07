namespace Core.Views.UI
{
    /// <summary>
    /// The single popup scene used for every notification (built in the editor).
    /// Receives the notification ID plus the region resolved from the message category,
    /// localizes the text itself and dismisses itself after a delay (Timed lifetime).
    /// </summary>
    public interface INotificationPopup : IPopup
    {
        void SetNotification(string notificationId, OverlayRegion region);
    }
}
