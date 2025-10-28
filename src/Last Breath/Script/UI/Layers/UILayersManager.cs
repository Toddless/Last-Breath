namespace LastBreath.Script.UI.Layers
{
    using Godot;
    using LastBreath.DIComponents;
    using Core.Interfaces.Mediator;
    using LastBreath.Script.Helpers;
    using Core.Interfaces.Mediator.Events;

    public partial class UILayersManager : Node
    {
        [Export] private CanvasLayer? _mainLayer, _windowLayer, _notificationLayer;

        private IUiMediator? _uiMediator;

        public override void _Ready()
        {
            var serviceProvider = GameServiceProvider.Instance;
            _uiMediator = serviceProvider.GetService<IUiMediator>();

        }

        public override void _UnhandledInput(InputEvent @event)
        {
            switch (true)
            {
                case var _ when @event.IsActionPressed(Settings.Inventory):
                    _uiMediator?.Publish(new OpenInventoryWindowEvent());
                    break;
                case var _ when @event.IsActionPressed(Settings.Quests):
                    _uiMediator?.Publish(new OpenQuestWindowEvent());
                    break;
                case var _ when @event.IsActionPressed(Settings.Character):
                    _uiMediator?.Publish(new OpenCharacterWindowEvent());
                    break;
                default: return;
            }
            GetViewport().SetInputAsHandled();
        }

        public void ShowHUD(Control hud) => _mainLayer?.CallDeferred(MethodName.AddChild, hud);

        public void ShowWindow(Control window) => _windowLayer?.CallDeferred(MethodName.AddChild, window);

        public void ShowNotification(Control notificaton) => _notificationLayer?.CallDeferred(MethodName.AddChild, notificaton);
    }
}
