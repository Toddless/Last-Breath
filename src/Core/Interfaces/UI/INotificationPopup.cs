namespace Core.Interfaces.UI
{
    /// <summary>
    /// The single popup scene used for every notification (built in the editor).
    /// Receives the notification ID and localizes the text itself; dismisses itself after a delay.
    /// </summary>
    public interface INotificationPopup
    {
        void SetNotification(string notificationId);
    }
}
