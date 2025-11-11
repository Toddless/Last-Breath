namespace Battle
{
    using Godot;
    using Services;
    using Core.Interfaces.Mediator;

    internal partial class UiLayerManager : Node
    {
        [Export] private CanvasLayer? _mainLayer, _windowLayer, _tooltipLayer, _notificationLayer;

        private IMediator? _mediator;

        public override void _Ready()
        {
            var serviceProvider = GameServiceProvider.Instance;
            _mediator = serviceProvider.GetService<IMediator>();
        }

        public void ShowMainElement(Control hud) => _mainLayer?.CallDeferred(MethodName.AddChild, hud);
        public void ShowWindowElement(Control window) => _windowLayer?.CallDeferred(MethodName.AddChild, window);
        public void ShowTooltipElement(Control tooltip) => _tooltipLayer?.CallDeferred(MethodName.AddChild, tooltip);
        public void ShowNotification(Control notification) => _notificationLayer?.CallDeferred(MethodName.AddChild, notification);
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
