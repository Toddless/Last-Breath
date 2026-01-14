namespace Core.Interfaces.Events
{
    public  record SendNotificationMessageEvent(string Message) : IEvent
    {
    }
}
