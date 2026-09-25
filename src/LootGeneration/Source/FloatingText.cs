namespace LootGeneration.Source
{
    using Godot;

    /// <summary>
    /// Short-lived world-space notice (e.g. "Inventory is full!") that floats up from an anchor
    /// and fades out. Pass an already localized string — callers own the Tr() lookup.
    /// </summary>
    public partial class FloatingText : Label
    {
        private const float RiseDistance = 45f;
        private const float Duration = 1.1f;

        public static FloatingText Spawn(Node2D anchor, string text, Color color)
        {
            var floatingText = new FloatingText
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                ZIndex = 100,
            };
            floatingText.AddThemeColorOverride("font_color", color);
            floatingText.AddThemeColorOverride("font_outline_color", Colors.Black);
            floatingText.AddThemeConstantOverride("outline_size", 6);
            floatingText.AddThemeFontSizeOverride("font_size", 20);
            anchor.AddChild(floatingText);
            return floatingText;
        }

        public override void _Ready()
        {
            // Godot computes the label's size only after it enters the tree — center it now.
            ResetSize();
            Position = new Vector2(-Size.X / 2f, -Size.Y - 40f);

            var tween = CreateTween().SetParallel();
            tween.TweenProperty(this, "position:y", Position.Y - RiseDistance, Duration).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(this, "modulate:a", 0f, Duration).SetEase(Tween.EaseType.In);
            tween.Chain().TweenCallback(Callable.From(QueueFree));
        }
    }
}
