namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using Core.Localization;
    using Godot;

    /// <summary>
    /// What catches a plan on its way out of the window. A plan costs nothing until it is confirmed, so
    /// closing on top of one loses work silently — and silence is the one thing an interface owes the
    /// player nothing for.
    ///
    /// <para>An ordinary node in the scene rather than a <see cref="ConfirmationDialog"/>: a native
    /// window falls outside the game's theme and outside the Esc-walks-the-layers model the rest of the
    /// interface is built on.</para>
    /// </summary>
    [GlobalClass]
    public partial class PassiveCloseGuard : PanelContainer
    {
        /// <summary>The uid the scene file declares in its own header — the authority, so a copy that
        /// drifts from it resolves to nothing at all in the game project.</summary>
        private const string UID = "uid://bcg8rtv5nqm3w";

        [Export] private Label? _title;
        [Export] private Label? _message;
        [Export] private Label? _refusal;
        [Export] private Button? _apply;
        [Export] private Button? _discard;
        [Export] private Button? _stay;

        public event Action? Applied;

        public event Action? Discarded;

        public event Action? Stayed;

        public override void _Ready()
        {
            _apply?.Pressed += () => Applied?.Invoke();
            _discard?.Pressed += () => Discarded?.Invoke();
            _stay?.Pressed += () => Stayed?.Invoke();

            _title?.Text = Localization.Localize(PassiveWheelText.CloseTitle);
            _apply?.Text = Localization.Localize(PassiveWheelText.CloseApply);
            _discard?.Text = Localization.Localize(PassiveWheelText.CloseDiscard);
            _stay?.Text = Localization.Localize(PassiveWheelText.CloseStay);
            Visible = false;
        }

        /// <summary>Puts the question on screen. A confirmation that cannot be taken is disabled with the
        /// reason printed under it — a dead button with nothing said beside it is the same silence this
        /// panel exists to break.</summary>
        public void Ask(string message, bool canApply, string? refusal)
        {
            _message?.Text = message;
            _apply?.Disabled = !canApply;

            if (_refusal != null)
            {
                _refusal.Text = refusal ?? string.Empty;
                _refusal.Visible = !string.IsNullOrEmpty(refusal);
            }

            Visible = true;
        }

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);
    }
}
