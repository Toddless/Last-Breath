namespace Core.Interfaces.Mediator.Events
{
    public  record SendNotificationMessageEvent(string Message) : IEvent
    {
    }
}
