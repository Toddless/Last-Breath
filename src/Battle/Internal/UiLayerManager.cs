namespace Battle.Internal
{
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.UI;
    using Godot;
    using Services;

    internal partial class UiLayerManager : Node, ILayerManager
    {
        [Export] private CanvasLayer? _mainLayer, _windowLayer, _tooltipLayer, _notificationLayer;

        private IGameMessageBus? _messageBus;

        public override void _Ready()
        {
            var serviceProvider = GameServiceProvider.Instance;
            _messageBus = serviceProvider.GetService<IGameMessageBus>();
        }

        public void ShowHud(IHud hud)
        {
            if (hud is Control cHud)
                _mainLayer?.CallDeferred(Node.MethodName.AddChild, cHud);
        }

        public void ShowWindow(IWindow window)
        {
            if (window is Control cWindow)
                _windowLayer?.CallDeferred(Node.MethodName.AddChild, cWindow);
        }

        public void ShowNotification(Control notification) =>
            _notificationLayer?.CallDeferred(Node.MethodName.AddChild, notification);

        public void CloseAllWindows()
        {
            foreach (var child in _windowLayer?.GetChildren() ?? [])
                _windowLayer?.CallDeferred(Node.MethodName.RemoveChild, child);
        }
    }
}
