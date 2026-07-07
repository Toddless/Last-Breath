namespace Core.Events
{
    /// <summary>What kind of notification this is; the UI maps categories to overlay regions.</summary>
    public enum NotificationCategory
    {
        /// <summary>System feed: saved, level up, ability unlocked.</summary>
        System,

        /// <summary>Prominent announcements: entering a location, boss encounters.</summary>
        Location,
    }

    /// <summary>Carries the notification ID; the popup localizes the text by it (see INotificationPopup).</summary>
    public record SendNotificationMessageMessage(string Id, NotificationCategory Category = NotificationCategory.System) : IMessage;
}
