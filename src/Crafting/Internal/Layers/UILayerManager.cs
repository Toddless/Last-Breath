namespace Crafting.Internal.Layers
{
    using Core.MessageBus;
    using Core.Views.UI;
    using Godot;
    using Services;

    [GlobalClass]
    internal partial class UILayerManager : Node, ILayerManager
    {
        [Export] private CanvasLayer? _mainLayer, _windowLayer, _notificationLayer;

        private IGameMessageBus? _messageBus;

        public override void _Ready()
        {
            var serviceProvider = GameServiceProvider.Instance;
            _messageBus = serviceProvider.GetService<IGameMessageBus>();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
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
