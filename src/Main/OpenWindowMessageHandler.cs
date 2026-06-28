namespace LastBreath
{
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.UI;

    public class OpenWindowMessageHandler(IUiElementsManager manager) : IMessageHandler<OpenWindowMessage>
    {
        public Task HandleMessageAsync(OpenWindowMessage message)
        {
            manager.OpenWindow(message.WindowType);
            return Task.CompletedTask;
        }
    }
}
