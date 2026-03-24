namespace Core.Interfaces.Events
{
    public  record SendNotificationMessageMessage(string Message) : IMessage
    {
    }
}
