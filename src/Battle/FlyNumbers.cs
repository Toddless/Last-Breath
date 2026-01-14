namespace Battle
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.UI;

    public partial class FlyNumbers : Node2D, IInitializable
    {
        private const string UID = "uid://crckxdaqepep8";
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
            return type switch
            {
                DamageType.Bleed => Colors.DarkRed,
                DamageType.Burning => Colors.OrangeRed,
                DamageType.Poison => Colors.LawnGreen,
                DamageType.Normal => isCritical ? Colors.Red : Colors.White,
                DamageType.Pure => Colors.Gold,
                _ => Colors.White
            };
        }
    }
}
