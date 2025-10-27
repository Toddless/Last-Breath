namespace LastBreath.Script.UI.Layers
{
    using Godot;

    public partial class UILayer : CanvasLayer
    {
        public void ShowWindow(Control window) => CallDeferred(MethodName.AddChild, window);
    }
}
