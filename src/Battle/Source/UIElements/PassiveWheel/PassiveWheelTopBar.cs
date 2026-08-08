namespace Battle.Source.UIElements.PassiveWheel
{
    using System;
    using System.Collections.Generic;
    using Core.Localization;
    using Godot;

    /// <summary>
    /// The strip above the wheel: what the character has, what the plan would do to it, and the four
    /// buttons that act on the plan.
    ///
    /// <para>It prints numbers and reports presses, and that is all. It holds no draft, no service and no
    /// bus — the window is the only thing that decides and the only thing that sends. So the same strip
    /// can be told about a plan that does not exist yet, and there is exactly one place where a press
    /// turns into an operation.</para>
    ///
    /// <para>The points counter shows the TRUTH and the plan beside it, never a blended number: the
    /// mastery screen reads the same counter, and a wheel showing "9" where mastery shows "12" would make
    /// one of the two a liar. What the plan would cost is a second, dimmer label.</para>
    /// </summary>
    [GlobalClass]
    public partial class PassiveWheelTopBar : HBoxContainer
    {
        /// <summary>The uid the scene file declares in its own header — the authority, so a copy that
        /// drifts from it resolves to nothing at all in the game project.</summary>
        private const string UID = "uid://ctb7pw2knr4qd";

        [Export] private Label? _title;
        [Export] private Label? _pointsCaption;
        [Export] private Label? _spentCaption;
        [Export] private Label? _totalCaption;
        [Export] private Label? _points;
        [Export] private Label? _pointsDelta;
        [Export] private Label? _spent;
        [Export] private Label? _total;
        [Export] private Label? _noPoints;
        [Export] private Label? _waiting;
        [Export] private Label? _price;

        [Export] private Button? _summary;
        [Export] private Button? _respec;
        [Export] private Button? _apply;
        [Export] private Button? _cancel;
        [Export] private Button? _frame;
        [Export] private Button? _close;

        public event Action<bool>? RespecToggled;

        public event Action<bool>? SummaryToggled;

        public event Action? ApplyPressed;

        public event Action? CancelPressed;

        public event Action? FramePressed;

        public event Action? ClosePressed;

        /// <summary>The standing words of the strip, written from code rather than left in the scene: the
        /// key list lives in one file (<see cref="PassiveWheelText"/>), and a caption typed into a scene
        /// is a key nothing can find again.</summary>
        public override void _Ready()
        {
            _title?.Text = Localization.Localize(PassiveWheelText.Title);
            _pointsCaption?.Text = Localization.Localize(PassiveWheelText.PointsCaption);
            _spentCaption?.Text = Localization.Localize(PassiveWheelText.SpentCaption);
            _totalCaption?.Text = Localization.Localize(PassiveWheelText.TotalCaption);

            _summary?.Text = Localization.Localize(PassiveWheelText.SummaryToggle);
            _respec?.Text = Localization.Localize(PassiveWheelText.RespecMode);
            _cancel?.Text = Localization.Localize(PassiveWheelText.Cancel);
            _frame?.Text = Localization.Localize(PassiveWheelText.Frame);
            _close?.Text = Localization.Localize(PassiveWheelText.Close);

            _summary?.Toggled += pressed => SummaryToggled?.Invoke(pressed);
            _respec?.Toggled += pressed => RespecToggled?.Invoke(pressed);
            _apply?.Pressed += () => ApplyPressed?.Invoke();
            _cancel?.Pressed += () => CancelPressed?.Invoke();
            _frame?.Pressed += () => FramePressed?.Invoke();
            _close?.Pressed += () => ClosePressed?.Invoke();
        }

        /// <summary>The three counters and the plan's effect on the first of them. The delta is hidden at
        /// zero rather than printed as "(0)": a plan that changes nothing has nothing to say.</summary>
        public void ShowPoints(int available, int spent, int total, int delta, bool givesBack)
        {
            _points?.Text = available.ToString();
            _spent?.Text = spent.ToString();
            _total?.Text = total.ToString();

            if (_pointsDelta == null) return;

            _pointsDelta.Visible = delta > 0;
            if (delta > 0)
                _pointsDelta.Text = Localization.Render(
                    givesBack ? PassiveWheelText.PointsDeltaRefund : PassiveWheelText.PointsDeltaTake,
                    new Dictionary<string, object?> { [PassiveWheelText.CountValue] = delta });
        }

        /// <summary>The character has been granted nothing to spend yet. Not the design budget beside it:
        /// the service measures every check against the total mastery granted, and a design ceiling
        /// printed next to it would be a promise nobody keeps.</summary>
        public void ShowNoPoints(bool visible)
        {
            if (_noPoints == null) return;

            _noPoints.Visible = visible;
            if (visible) _noPoints.Text = Localization.Localize(PassiveWheelText.NoPoints);
        }

        /// <summary>How many augments are sitting in slots no node backs any more — the one place the
        /// player learns there is property of his waiting in closed cells.</summary>
        public void ShowWaiting(int count)
        {
            if (_waiting == null) return;

            _waiting.Visible = count > 0;
            if (count > 0)
                _waiting.Text = Localization.Render(PassiveWheelText.AugmentsWaiting,
                    new Dictionary<string, object?> { [PassiveWheelText.CountValue] = count });
        }

        public void ShowApply(string text, bool enabled)
        {
            if (_apply == null) return;

            _apply.Text = text;
            _apply.Disabled = !enabled;
        }

        /// <summary>The live price of the plan, coloured by whether the purse covers it. Empty text hides
        /// it whole: there is no price to show while nothing is being given back.</summary>
        public void ShowPrice(string text, bool affordable)
        {
            if (_price == null) return;

            _price.Text = text;
            _price.Visible = text.Length > 0;
            _price.Modulate = affordable ? Colors.White : Color.FromHtml(TextPalette.Debuff);
        }

        public void ShowCancel(bool visible) => _cancel?.Visible = visible;

        public void SetRespecEnabled(bool enabled) => _respec?.Disabled = !enabled;

        /// <summary>Mirrors a mode the window decided on, without reporting it back: the toggle signal is
        /// suppressed while the pressed state is written, so a window correcting the strip cannot loop
        /// through it.</summary>
        public void SetRespecPressed(bool pressed) => SetPressed(_respec, pressed);

        public void SetSummaryPressed(bool pressed) => SetPressed(_summary, pressed);

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        private static void SetPressed(BaseButton? button, bool pressed) =>
            button?.SetPressedNoSignal(pressed);
    }
}
