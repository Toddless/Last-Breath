namespace Core.Events
{
    /// <summary>Carries the notification ID; the popup localizes the text by it (see INotificationPopup).</summary>
    public record SendNotificationMessageMessage(string Id) : IMessage
    {
    }
}
