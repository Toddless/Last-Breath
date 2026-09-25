namespace LastBreath.UI
{
    using System.Threading.Tasks;
    using Core.Localization;
    using Core.Services;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// The single notification popup: one visual for every notification, only the text changes.
    /// The service creates it via the factory, calls <see cref="SetNotification"/> with a localization
    /// key, then adds it to the notification layer. Text and auto-dismiss are applied in <see cref="_Ready"/>
    /// (once the node is in the tree — the service calls SetNotification BEFORE attaching it).
    ///
    /// Scene ownership: build the .tscn in Godot, attach this script, bind the text Label to the
    /// <c>_label</c> [Export], then paste the scene UID into <see cref="UID"/>. The script base is
    /// <see cref="Control"/> so the scene root can be any Control-derived node (Panel, PanelContainer…).
    /// </summary>
    public partial class NotificationPopup : Control, INotificationPopup
    {
        private const string UID = "uid://b3e1nusmv0gds";
        private const float FadeSeconds = 0.5f;
        private const float HoldSeconds = 3.0f;

        // Button to close early
        [Export] private Button? _close;
        [Export] private Label? _label;
        private string _text = string.Empty;

        public PopupLifetime Lifetime => PopupLifetime.Timed;

        /// <summary>Resolved by the NotificationService from the message category before showing.</summary>
        public OverlayRegion Region { get; private set; }

        /// <summary>Localizes and stores the text; display starts in _Ready, once the node is in the tree.</summary>
        public void SetNotification(NotificationContent content, OverlayRegion region)
        {
            _text = content.Values == null
                ? Localization.Localize(content.Id)
                : Localization.Render(content.Id, content.Values);
            Region = region;
        }

        public void Close() => QueueFree();

        public override void _Ready()
        {
            _label?.Text = _text;
            _close?.Pressed += Close;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public async Task PlayLifecycleAsync()
        {
            // The service starts the lifecycle right after ShowOverlay, but the layer adds the
            // node DEFERRED — tweens and timers need the tree, so wait for _Ready first
            if (!IsInsideTree()) await ToSignal(this, Node.SignalName.Ready);

            Modulate = new Color(Modulate, 0);

            var fadeIn = CreateTween();
            fadeIn.TweenProperty(this, "modulate:a", 1.0f, FadeSeconds);
            await ToSignal(fadeIn, Tween.SignalName.Finished);

            await ToSignal(GetTree().CreateTimer(HoldSeconds), SceneTreeTimer.SignalName.Timeout);

            var fadeOut = CreateTween();
            fadeOut.TweenProperty(this, "modulate:a", 0.0f, FadeSeconds);
            await ToSignal(fadeOut, Tween.SignalName.Finished);

            QueueFree();
        }
    }
}
