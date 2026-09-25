namespace Battle.Source.UIElements
{
    using System.Collections.Generic;
    using Core.Localization;
    using Core.Views;
    using Core.Views.UI;
    using Godot;

    public partial class EffectSlot : Control, IInitializable
    {
        private const string UID = "uid://5n5bfrh72v8s";

        // An autowrap label needs its width pinned, or the min-size pass measures the height
        // at one word per line and balloons the tooltip.
        private const int TooltipLineWidth = 320;

        private EffectView? _view;
        [Export] private TextureRect? _effectIcon;
        [Export] private Label? _effectStacks;

        public string EffectId { get; private set; } = string.Empty;

        /// <summary>Tinted name with the stack count, the effect's own rich description and the
        /// remaining duration (hidden for permanent effects).</summary>
        public override GodotObject _MakeCustomTooltip(string forText)
        {
            var layout = new VBoxContainer();
            layout.AddThemeConstantOverride("separation", 4);
            if (_view is not { } view) return layout;

            string name = Core.Localization.Localization.Localize(view.Id);
            var title = new Label { Text = view.Stacks > 1 ? $"{name} x{view.Stacks}" : name };
            title.AddThemeColorOverride("font_color", Color.FromHtml(view.IsHarmful ? TextPalette.Debuff : TextPalette.Buff));
            layout.AddChild(title);

            layout.AddChild(new RichTextLabel
            {
                Text = view.Description,
                BbcodeEnabled = true,
                FitContent = true,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(TooltipLineWidth, 0),
            });

            if (view.Duration > 0)
            {
                layout.AddChild(new Label
                {
                    Text = Core.Localization.Localization.Render("UI_Effect_Duration",
                        new Dictionary<string, object?> { ["Duration"] = view.Duration }),
                    ThemeTypeVariation = "DimLabel",
                });
            }

            return layout;
        }

        /// <summary>Binds the aggregated view: icon, stack count (hidden when 1) and remaining duration.</summary>
        public void SetView(EffectView view)
        {
            _view = view;
            EffectId = view.Id;
            _effectIcon?.Texture = view.Icon;
            _effectStacks?.Text = view.Stacks <= 1 ? string.Empty : view.Stacks.ToString();
            // An empty tooltip_text suppresses the engine's tooltip pass entirely — the actual
            // content comes from _MakeCustomTooltip, the text only has to be non-empty.
            TooltipText = view.Id;
        }

        public void RemoveEffect()
        {
            EffectId = string.Empty;
            QueueFree();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
