namespace Battle
{
    using Godot;
    using Battle.Services;
    using Core.Interfaces.Mediator;

    internal partial class UILayerManager : Node
    {
        [Export] private CanvasLayer? _mainLayer, _windowLayer, _tooltipLayer, _notificationLayer;

        private IUiMediator? _uiMediator;

        public override void _Ready()
        {
            var serviceProvider = GameServiceProvider.Instance;
            _uiMediator = serviceProvider.GetService<IUiMediator>();
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
