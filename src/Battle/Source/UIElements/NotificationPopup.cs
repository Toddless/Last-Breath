namespace Battle.Source.UIElements
{
    using System;
    using Core.Interfaces.UI;
    using Godot;
    using Utilities;

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
        // TODO(Todd): replace with the real UID after creating NotificationPopup.tscn.
        private const string UID = "uid://b3e1nusmv0gds";
        private const float FadeSeconds = 0.4f;
        private const float HoldSeconds = 2.0f;

        // Button to close early
        [Export] private Button? _close;
        [Export] private Label? _label;
        private string _text = string.Empty;

        /// <summary>Localizes and stores the text; display starts in _Ready, once the node is in the tree.</summary>
        public void SetNotification(string notificationId) => _text = Localization.Localize(notificationId);

        public override void _Ready()
        {
            _label?.Text = _text;
            _close?.Pressed += OnPressed;
            PlayLifecycleAsync();
        }

        private void OnPressed() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private async void PlayLifecycleAsync()
        {
            try
            {
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
            catch (Exception ex)
            {
                Tracker.TrackException($"Failed to play the animation: {ex.Message}", ex, this);
                GD.Print($"Failed to play the animation: {ex.Message}, {ex.StackTrace}");
            }
        }
    }
}
