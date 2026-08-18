namespace Battle.Source.UIElements
{
    using Core.Enums;
    using Core.Localization;
    using Core.Views.UI;
    using Godot;

    public partial class FlyNumbers : Node2D, IInitializable
    {
        private const string UID = "uid://b3me1ofsbyw38";
        [Export] private Label? _label;
        [Export] private float _rise = 25f;
        [Export] private float _duration = 0.5f;

        public void PlayDamageNumbers(int value, DamageType type, bool isCritical = false)
        {
            if (isCritical) Scale = Vector2.One * 1.2f;
            PlayFloat(value.ToString(), DefineColor(type, isCritical));
        }

        public void PlayHealNumbers(int value) =>
            PlayFloat(value.ToString(), Colors.LawnGreen)
                .Parallel().TweenProperty(this, "scale", Vector2.One * 1.3f, _duration);

        /// <summary>Floating status text (e.g. the skip-turn reason) instead of a number.</summary>
        public void PlayStatusText(string text) => PlayFloat(text, Colors.LightGray);

        /// <summary>The shared rise-and-fade; returned so a caller can append parallel steps.</summary>
        private Tween PlayFloat(string text, Color color)
        {
            _label?.Text = text;
            _label?.Modulate = color;

            var tween = CreateTween();
            tween.TweenProperty(this, "position:y", Position.Y - _rise, _duration);
            tween.Parallel().TweenProperty(this, "modulate:a", 0f, _duration);
            tween.Finished += QueueFree;
            return tween;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private Color DefineColor(DamageType type, bool isCritical = false)
        {
            return isCritical
                ? Colors.Crimson
                : type switch
                {
                    DamageType.Bleed => Colors.DarkRed,
                    DamageType.Burning => Colors.OrangeRed,
                    DamageType.Poison => Colors.LawnGreen,
                    DamageType.Fire => Colors.Orange,
                    DamageType.Cold => Colors.LightSkyBlue,
                    DamageType.Lightning => Colors.Blue,
                    DamageType.Sacred => Colors.Gold,
                    // No named colour sits near the blight; it borrows the log palette's swamp purple.
                    DamageType.Blight => Color.FromHtml(TextPalette.DamageColor(DamageType.Blight)),
                    // Physical, and anything a rule forgets: white is what plain Physical reads as, so an
                    // unlisted type shows an untinted number instead of borrowing another type's colour.
                    _ => Colors.White
                };
        }
    }
}
