namespace Battle.Source.UIElements
{
    using Core.Enums;
    using Core.Interfaces.UI;
    using Godot;

    public partial class FlyNumbers : Node2D, IInitializable
    {
        private const string UID = "uid://b3me1ofsbyw38";
        [Export] private Label? _label;
        [Export] private float _rise = 25f;
        [Export] private float _duration = 0.5f;

        public void PlayDamageNumbers(int value, DamageType type, bool isCritical = false)
        {
            _label?.Text = value.ToString();
            _label?.Modulate = DefineColor(type, isCritical);

            if (isCritical)
                Scale = Vector2.One * 1.2f;

            var tween = CreateTween();
            tween.TweenProperty(this, "position:y", Position.Y - _rise, _duration);
            tween.Parallel().TweenProperty(this, "modulate:a", 0f, _duration);
            tween.Finished += QueueFree;
        }

        public void PlayHealNumbers(int value)
        {
            _label?.Text = value.ToString();
            _label?.Modulate = Colors.LawnGreen;

            var tween = CreateTween();
            tween.TweenProperty(this, "position:y", Position.Y - _rise, _duration);
            tween.Parallel().TweenProperty(this, "modulate:a", 0f, _duration);
            tween.Parallel().TweenProperty(this, "scale", Vector2.One * 1.3f, _duration);
            tween.Finished += QueueFree;
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
                    DamageType.Pure => Colors.Gold,
                    _ => Colors.White // Physical
                };
        }
    }
}
