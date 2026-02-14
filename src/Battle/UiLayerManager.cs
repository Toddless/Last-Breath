namespace Battle
{
    using Core.Interfaces.MessageBus;
    using Godot;
    using Services;
    using TestData;

    internal partial class UiLayerManager : Node
    {
        [Export] private CanvasLayer? _mainLayer, _windowLayer, _tooltipLayer, _notificationLayer;

        private IGameMessageBus? _mediator;

        public override void _Ready()
        {
            var serviceProvider = GameServiceProvider.Instance;
            _mediator = serviceProvider.GetService<IGameMessageBus>();
            var devPanel = DevPanel.Initialize().Instantiate<DevPanel>();
        }

        public void ShowMainElement(Control hud) => _mainLayer?.CallDeferred(Node.MethodName.AddChild, hud);
        public void ShowWindowElement(Control window) => _windowLayer?.CallDeferred(Node.MethodName.AddChild, window);
        public void ShowTooltipElement(Control tooltip) => _tooltipLayer?.CallDeferred(Node.MethodName.AddChild, tooltip);
        public void ShowNotification(Control notification) => _notificationLayer?.CallDeferred(Node.MethodName.AddChild, notification);
        public void RemoveMainElement(Control hud) => _mainLayer?.CallDeferred(Node.MethodName.RemoveChild, hud);
        public void RemoveWindowElement(Control window) => _windowLayer?.CallDeferred(Node.MethodName.RemoveChild, window);
        public void RemoveTooltipElement(Control tooltip) => _tooltipLayer?.CallDeferred(Node.MethodName.RemoveChild, tooltip);

        public void CloseAllWindows()
        {
            foreach (var child in _windowLayer?.GetChildren() ?? [])
                _windowLayer?.CallDeferred(Node.MethodName.RemoveChild, child);
        }
    }
}
