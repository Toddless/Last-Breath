namespace Crafting.Source.EventHandlers
{
    using UIElements;
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using System.Collections.Generic;
    using Core.Data;

    public class SendNotificationMessageEventHandler(IUiElementProvider uIElementProvider)
        : IEventHandler<SendNotificationMessageEvent>
    {
        private readonly Queue<string> _msgQueue = [];
        private bool _isProcessed = false;

        public async Task HandleAsync(SendNotificationMessageEvent evnt)
        {
            _msgQueue.Enqueue(evnt.Message);
            await ProcessMessagesAsync();
        }

        private async Task ProcessMessagesAsync()
        {
            if (_isProcessed) return;
            _isProcessed = true;
            try
            {
                while (_msgQueue.Count > 0)
                {
                    string msg = _msgQueue.Dequeue();
                    var notification = uIElementProvider.CreateAndShowNotification<NotificationMessage>();
                    notification.SetMessage(msg);
                    await notification.WaitForCloseAsync();
                }
            }
            finally
            {
                _isProcessed = false;
            }
        }
    }
}
