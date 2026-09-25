namespace SharedUi
{
    using System;
    using Godot;

    /// <summary>
    /// A window's title bar: gold header text on the left, an "X" close button on the right.
    /// The host window subscribes to <see cref="Closed"/> and hides/frees itself.
    /// </summary>
    public partial class WindowHeader : HBoxContainer
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://q1mb3d00x8m8";

        [Export] private Label? _title;
        [Export] private Button? _close;

        /// <summary>Raised when the close button is pressed.</summary>
        public event Action? Closed;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public override void _Ready() => _close?.Pressed += OnClosePressed;

        public void SetTitle(string title) => _title?.Text = title;

        private void OnClosePressed() => Closed?.Invoke();
    }
}
