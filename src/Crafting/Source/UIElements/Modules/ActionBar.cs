namespace Crafting.Source.UIElements.Modules
{
    using System;
    using Godot;

    /// <summary>The bar under the bench: one action button on the right. It reports presses and
    /// prints what it is told — the window decides what the press means.</summary>
    [GlobalClass]
    public partial class ActionBar : HBoxContainer
    {
        [Export] private Button? _action;

        public event Action? ActionPressed;

        public override void _Ready() => _action?.Pressed += () => ActionPressed?.Invoke();

        /// <summary>What the one button would do and whether it currently can.</summary>
        public void SetAction(string text, bool enabled)
        {
            if (_action == null) return;

            _action.Text = text;
            _action.Disabled = !enabled;
        }
    }
}
