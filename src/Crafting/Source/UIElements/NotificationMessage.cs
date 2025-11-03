namespace Crafting.Source.UIElements
{
    using Godot;
    using Core.Interfaces.UI;
    using System.Threading.Tasks;

    public partial class NotificationMessage : Control, IInitializable
    {
        private TaskCompletionSource<object?>? _closeTcs;

        private const string UID = "uid://dsfb7ik6bw6h3";
        private readonly float _showTweenDuration = 0.3f;
        private readonly float _visibleDuration = 2f;
        private readonly float _hideTweenDuration = 0.05f;
        private Tween? _runningTween;

        [Export] private Label? _msgLabel;
        [Export] private float _moveDistance = -100f;

        public override void _Ready()
        {
            StartAnimation();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void SetMessage(string message) => _msgLabel.Text = message;

        public Task WaitForCloseAsync()
        {
            if (_closeTcs is { Task.IsCompleted: false } existing)
                return existing.Task;
            _closeTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return _closeTcs.Task;
        }

        public override void _ExitTree()
        {
            _closeTcs?.TrySetResult(null);
        }

        private void StartAnimation()
        {
            Vector2 startPos = Position;
            Vector2 endPos = startPos + new Vector2(0, _moveDistance);

            Modulate = new Color(1, 1, 1, 0);

            _runningTween?.Kill();
            _runningTween = CreateTween();

            _runningTween.Parallel().TweenProperty(this, "modulate", new Color(1, 1, 1, 1), _showTweenDuration)
                         .SetTrans(Tween.TransitionType.Cubic)
                         .SetEase(Tween.EaseType.Out);
            _runningTween.Parallel().TweenProperty(this, "position", endPos, _showTweenDuration + _visibleDuration + _hideTweenDuration)
                         .SetTrans(Tween.TransitionType.Cubic)
                         .SetEase(Tween.EaseType.Out);
            _runningTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), _hideTweenDuration)
                         .SetTrans(Tween.TransitionType.Cubic)
                         .SetEase(Tween.EaseType.In);

            _runningTween.TweenCallback(Callable.From(QueueFree));
        }
    }
}
