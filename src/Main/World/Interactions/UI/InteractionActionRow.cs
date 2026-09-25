namespace LastBreath.World.Interactions.UI
{
    using System;
    using Core.Localization;
    using Core.World.Interactions;
    using Godot;

    /// <summary>One action of the interaction menu: its label, a disabled action's reason and the danger look of an explicit choice.</summary>
    public partial class InteractionActionRow : Button
    {
        private const string UID = "uid://bach43uthyem6";
        /// <summary>Theme variation of an action the player has to choose explicitly.</summary>
        private const string DangerVariation = "DangerButton";
        [Export] private Label? _reason;

        /// <summary>ID of the shown action; empty until <see cref="SetAction"/>.</summary>
        public string ActionId { get; private set; } = string.Empty;

        /// <summary>The player pressed the row; carries the ID of the shown action.</summary>
        public event Action<string>? ActionPressed;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public override void _Ready() => Pressed += OnPressed;

        /// <summary>Shows the action: label, availability, the danger look of an explicit choice and, while disabled, its reason.</summary>
        public void SetAction(InteractionAction action)
        {
            ActionId = action.Id;
            Text = Localization.Localize(action.LabelKey);
            Disabled = !action.Enabled;
            ThemeTypeVariation = action.ExplicitChoice ? DangerVariation : string.Empty;
            string reason = action.ReasonKey == null ? string.Empty : Localization.Localize(action.ReasonKey);
            TooltipText = reason;
            _reason?.Text = reason;
            _reason?.Visible = !action.Enabled && reason.Length > 0;
        }

        private void OnPressed() => ActionPressed?.Invoke(ActionId);
    }
}
