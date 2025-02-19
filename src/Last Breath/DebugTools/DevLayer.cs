namespace Playground.Script.UI
{
    using Godot;
    using Playground.DebugTools;

    public partial class DevLayer : CanvasLayer
    {
        private DevTools? _dev;

        public override void _Ready()
        {
            _dev = GetNode<DevTools>(nameof(DevTools));
        }

        public override void _ExitTree()
        {
            foreach (var child in _dev!.GetChildren())
            {
                child.QueueFree();
            }
            base._ExitTree();
        }
    }
}
