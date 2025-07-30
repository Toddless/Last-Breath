namespace LastBreath.Script.UI
{
    using Godot;
    using LastBreath.Script;
    using LastBreath.Script.Helpers;

    public partial class TurnNotifier : Panel
    {
        private const int Offset = 25;
        [Export] private Label? _textLabel;


        [Signal] public delegate void CompletedEventHandler();

        public async void ShowMessage(ICharacter character)
        {
            if (_textLabel == null)
            {
                EmitSignal(SignalName.Completed);
                QueueFree();
                return;
            }

            var screenSize = GetViewportRect().Size;
            Position = new Vector2((screenSize.X - Size.X) / 2, Offset);
            _textLabel.Text = $"{character.CharacterName}´s turn.";

            Modulate = new Color(Modulate, 0);
            var tween = CreateTween();

            tween.TweenProperty(this, "modulate:a", 1.0f, 0.4f);
            await ToSignal(tween, "finished");

            await ToSignal(GetTree().CreateTimer(1.5), "timeout");

            tween = CreateTween();

            tween.TweenProperty(this, "modulate:a", 0.0f, 0.4f);
            await ToSignal(tween, "finished");
            EmitSignal(SignalName.Completed);
            QueueFree();
        }

        public static PackedScene InitializeAsPackedScene() => ResourceLoader.Load<PackedScene>(ScenePath.TurnNotifier);
    }
}
