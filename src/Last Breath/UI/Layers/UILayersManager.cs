namespace LastBreath.Source.UI.Layers
{
    using System;
    using Godot;
    using Services;
    using Core.Constants;
    using Core.Interfaces.Events;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.UI;

    public partial class UILayersManager : Node
    {
        [Export] private CanvasLayer? _mainLayer, _windowLayer, _notificationLayer;

        private IGameMessageBus? _gameMessageBus;

        public override void _Ready()
        {
            var serviceProvider = GameServiceProvider.Instance;
            _gameMessageBus = serviceProvider.GetService<IGameMessageBus>();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            switch (true)
            {
                case var _ when @event.IsActionPressed(Settings.Inventory):
                    _gameMessageBus?.PublishMessageAsync(new OpenInventoryWindowMessage());
                    break;
                case var _ when @event.IsActionPressed(Settings.Quests):
                    _gameMessageBus?.PublishMessageAsync(new OpenQuestWindowMessage());
                    break;
                case var _ when @event.IsActionPressed(Settings.Character):
                    _gameMessageBus?.PublishMessageAsync(new OpenCharacterWindowMessage());
                    break;
                case var _ when @event.IsActionPressed(Settings.Cancel):
                    CloseAllWindows();
                    _gameMessageBus?.PublishMessageAsync(new PauseGameMessage());
                    break;
                default: return;
            }

            GetViewport().SetInputAsHandled();
        }

        public void ShowHud(IHud hud)
        {
            if (hud is Control cHud) _mainLayer?.CallDeferred(Node.MethodName.AddChild, cHud);
        }

        public void ShowWindow(IWindow window)
        {
            if (window is Control cWindow) _windowLayer?.CallDeferred(Node.MethodName.AddChild, cWindow);
        }

        public void ShowNotification(Control notification) => _notificationLayer?.CallDeferred(Node.MethodName.AddChild, notification);
        public void RemoveMainElement(Control hud) => _mainLayer?.CallDeferred(Node.MethodName.RemoveChild, hud);
        public void RemoveWindowElement(Control window) => _windowLayer?.CallDeferred(Node.MethodName.RemoveChild, window);

        public void CloseAllWindows()
        {
            foreach (var child in _windowLayer?.GetChildren() ?? [])
                _windowLayer?.CallDeferred(Node.MethodName.RemoveChild, child);
        }
    }
}
