namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Crafting.Source;
    using Core.Interfaces.Mediator;
    using Crafting.Source.UIElements;
    using System.Collections.Generic;
    using Core.Interfaces.Mediator.Events;

    internal class SendNotificationMessageEventHandler : IEventHandler<SendNotificationMessageEvent>
    {
        private readonly UIElementProvider _uIElementProvider;
        private readonly Queue<string> _msgQueue = [];
        private bool _isProcessed = false;

        public SendNotificationMessageEventHandler(UIElementProvider uIElementProvider)
        {
            _uIElementProvider = uIElementProvider;
        }

        public void Handle(SendNotificationMessageEvent evnt)
        {
            _msgQueue.Enqueue(evnt.Message);
            ProcessMessagesAsync();
        }

        private async void ProcessMessagesAsync()
        {
            if (_isProcessed) return;
            _isProcessed = true;
            try
            {
                while (_msgQueue.Count > 0)
                {
                    var msg = _msgQueue.Dequeue();
                    var notification = _uIElementProvider.CreateAndShowOnUI<NotificationMessage>();
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
