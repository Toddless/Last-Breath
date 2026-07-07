namespace Core.Events
{
    using System.Threading.Tasks;

    public interface IMessageHandler<TMessage>
        where TMessage : IMessage
    {
        Task HandleMessageAsync(TMessage message);
    }
}
