namespace LastBreath.Script.UI.Layers
{
    using Godot;
    using LastBreath.DIComponents;
    using Core.Constants;
    using Core.Interfaces.Events;
    using Core.Interfaces.MessageBus;

    public partial class UILayersManager : Node
    {
        [Export] private CanvasLayer? _mainLayer, _windowLayer, _tooltipLayer, _notificationLayer;

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
                    _gameMessageBus?.PublishAsync(new OpenInventoryWindowEvent());
                    break;
                case var _ when @event.IsActionPressed(Settings.Quests):
                    _gameMessageBus?.PublishAsync(new OpenQuestWindowEvent());
                    break;
                case var _ when @event.IsActionPressed(Settings.Character):
                    _gameMessageBus?.PublishAsync(new OpenCharacterWindowEvent());
                    break;
                case var _ when @event.IsActionPressed(Settings.Cancel):
                    CloseAllWindows();
                    _gameMessageBus?.PublishAsync(new PauseGameEvent());
                    break;
                default: return;
            }

            GetViewport().SetInputAsHandled();
        }

        public void ShowMainElement(Control hud) => _mainLayer?.CallDeferred(MethodName.AddChild, hud);
        public void ShowWindowElement(Control window) => _windowLayer?.CallDeferred(MethodName.AddChild, window);
        public void ShowTooltipElement(Control tooltip) => _tooltipLayer?.CallDeferred(MethodName.AddChild, tooltip);
        public void ShowNotification(Control notificaton) => _notificationLayer?.CallDeferred(MethodName.AddChild, notificaton);
        public void RemoveMainElement(Control hud) => _mainLayer?.CallDeferred(MethodName.RemoveChild, hud);
        public void RemoveWindowElement(Control window) => _windowLayer?.CallDeferred(MethodName.RemoveChild, window);
        public void RemoveTooltipElement(Control tooltip) => _tooltipLayer?.CallDeferred(MethodName.RemoveChild, tooltip);

        public void CloseAllWindows()
        {
            foreach (var child in _windowLayer?.GetChildren() ?? [])
                _windowLayer?.CallDeferred(MethodName.RemoveChild, child);
        }
    }
}
