namespace Playground.Script.UI.View
{
    using Godot;

    public partial class FloatingText : Label
    {
        private readonly RandomNumberGenerator _rnd = new();

        public async void ShowValue(string value, Vector2 travel, float duration, float spread, bool crit = false)
        {
            this.Text = value;
            PivotOffset = Size / 2;
            float angle = _rnd.RandfRange(-spread / 2, spread / 2);
            var movement = travel.Rotated(angle);

            var tween = GetTree().CreateTween();
            tween.TweenProperty(this, "position", Position + movement, duration);
            tween.TweenProperty(this, "modulate:a", 0, duration * 0.2f).SetDelay(duration * 0.1f);
            if (crit)
            {
                Modulate = Colors.Red;
                tween.TweenProperty(this, "scale", Scale * 2, duration);
            }

            tween.Play();
            await ToSignal(tween, "finished");
            this.QueueFree();
        }
    }
}
