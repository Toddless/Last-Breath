namespace LastBreath
{
    using Core.Interfaces.UI;
    using System.Threading.Tasks;
    using Core.Interfaces.Events;

    public class OpenWindowMessageHandler(IUiElementsManager manager) : IMessageHandler<OpenWindowMessage>
    {
        public Task HandleMessageAsync(OpenWindowMessage message)
        {
            manager.OpenWindow(message.WindowType);
            return Task.CompletedTask;
        }
    }
}
