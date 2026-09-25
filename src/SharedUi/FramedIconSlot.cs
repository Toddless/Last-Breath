namespace SharedUi
{
    using Godot;

    /// <summary>
    /// A hairline-framed icon slot (56x56, aspect kept). <see cref="SetTint"/> recolors the frame
    /// border — the rarity tint — without touching the shared theme stylebox.
    /// </summary>
    public partial class FramedIconSlot : PanelContainer
    {
        // The uid rather than the path: the same file is mounted into every project, and only the
        // uid resolves the scene from all of them.
        private const string UID = "uid://c6jc6tqvd4kk7";

        [Export] private TextureRect? _icon;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void SetIcon(Texture2D? icon) => _icon?.Texture = icon;

        /// <summary>Tints the frame border (e.g. with the item's rarity color).</summary>
        public void SetTint(Color color)
        {
            if (GetThemeStylebox("panel") is not StyleBoxFlat frame) return;
            var tinted = (StyleBoxFlat)frame.Duplicate();
            tinted.BorderColor = color;
            AddThemeStyleboxOverride("panel", tinted);
        }
    }
}
