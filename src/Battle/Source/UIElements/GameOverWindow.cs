namespace Battle.Source.UIElements
{
    using Core.Data;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The final-death screen (the corpse was burned): load a save or quit. No close button —
    /// there is nothing to return to. The scene is a bare frame; restyle freely later.
    /// </summary>
    [GlobalClass]
    public partial class GameOverWindow : Control, IWindow
    {
        private const string UID = "uid://dc3x7abnrt5pr";

        [Export] private Button? _loadButton;
        [Export] private Button? _quitButton;

        private IUiElementsManager? _uiElements;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            if (_loadButton != null) _loadButton.Pressed += OpenSaveLoad;
            if (_quitButton != null) _quitButton.Pressed += () => GetTree().Quit();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _uiElements = provider.GetService<IUiElementsManager>();
        }

        public void Close() => GetParent().RemoveChild(this);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        // The save/load window on top: save buttons are disabled (CanSave requires a living
        // player), so only loading is actionable — exactly what a game over offers.
        private void OpenSaveLoad() => _uiElements?.GetOrOpenWindow(typeof(SaveLoadWindow));
    }
}
