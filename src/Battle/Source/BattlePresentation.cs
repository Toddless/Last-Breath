namespace Battle.Source
{
    using System;
    using Godot;

    /// <summary>Displays a transient physical space while its origin keeps simulating.</summary>
    internal sealed class BattlePresentation : IDisposable
    {
        private readonly Window _window;
        private readonly SubViewportContainer _view;
        private readonly Viewport _originViewport;
        private readonly CanvasItem _originView;
        private readonly bool _originVisible;
        private readonly bool _originInputDisabled;
        private readonly bool _originListener;
        private bool _disposed;

        public SubViewport Viewport { get; }

        public BattlePresentation(Node2D origin, Node parent)
        {
            _window = parent.GetTree().Root;
            _originViewport = origin.GetViewport();
            _originView = _originViewport.GetParent() as SubViewportContainer ?? (CanvasItem)origin;
            _originVisible = _originView.Visible;
            _originInputDisabled = _originViewport.GuiDisableInput;
            _originListener = _originViewport.AudioListenerEnable2D;
            _view = new SubViewportContainer { Name = "BattleView", Stretch = true, MouseFilter = Control.MouseFilterEnum.Pass };
            Viewport = new SubViewport
            {
                Name = "BattleSpace",
                World2D = new World2D(),
                PhysicsObjectPicking = true,
                AudioListenerEnable2D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                CanvasItemDefaultTextureFilter = _originViewport.CanvasItemDefaultTextureFilter,
                Msaa2D = _originViewport.Msaa2D,
                UseHdr2D = _originViewport.UseHdr2D
            };
            _view.AddChild(Viewport);
            Resize();
            _window.SizeChanged += Resize;
            _originView.Hide();
            if (_originViewport is SubViewport) _originViewport.GuiDisableInput = true;
            _originViewport.AudioListenerEnable2D = false;
            parent.CallDeferred(Node.MethodName.AddChild, _view);
        }

        private void Resize()
        {
            _view.Size = _window.GetVisibleRect().Size;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _window.SizeChanged -= Resize;
            _originView.Visible = _originVisible;
            _originViewport.GuiDisableInput = _originInputDisabled;
            _originViewport.AudioListenerEnable2D = _originListener;
            _view.QueueFree();
        }
    }
}
