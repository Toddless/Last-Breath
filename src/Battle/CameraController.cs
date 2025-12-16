namespace Battle
{
    using Core.Interfaces;
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class CameraController : Node
    {

        [Export] private Camera2D?  _camera2D;
        private ICameraFocus? _currentFocus;

        public void SetFocus(ICameraFocus focus)
        {
            _currentFocus = focus;
            _camera2D?.GlobalPosition = focus.GetCameraPosition();
        }


        public override void _Process(double delta)
        {
            if(_currentFocus != null)
                _camera2D?.GlobalPosition = _currentFocus.GetCameraPosition();
        }
    }
}
