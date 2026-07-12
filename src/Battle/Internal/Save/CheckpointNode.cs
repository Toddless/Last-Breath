namespace Battle.Internal.Save
{
    using Core.Services;
    using Core.Views.UI;
    using Godot;
    using GameServiceProvider = Services.GameServiceProvider;

    /// <summary>
    /// A checkpoint in the world: clicking it with the player standing nearby (same reach as
    /// burning bodies) opens the save/load window. The visual is authored in the scene.
    /// </summary>
    [GlobalClass]
    internal partial class CheckpointNode : Node2D
    {
        private const float InteractDistance = 150f;

        [Export] private Area2D? _interactionArea;
        private IUiElementsManager? _uiElements;
        private IPlayerAccessor? _playerAccessor;

        public override void _Ready()
        {
            _uiElements = GameServiceProvider.Instance.GetService<IUiElementsManager>();
            _playerAccessor = GameServiceProvider.Instance.GetService<IPlayerAccessor>();
            if (_interactionArea != null) _interactionArea.InputEvent += OnInteractionAreaInput;
        }

        private void OnInteractionAreaInput(Node viewport, InputEvent @event, long shapeIdx)
        {
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            if (!IsPlayerWithin(InteractDistance)) return;

            _uiElements?.ToggleWindow(typeof(SaveLoadWindow));
        }

        private bool IsPlayerWithin(float distance) =>
            _playerAccessor?.Player is Node2D playerNode && GlobalPosition.DistanceTo(playerNode.GlobalPosition) <= distance;
    }
}
