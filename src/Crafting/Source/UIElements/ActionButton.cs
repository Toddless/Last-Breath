namespace Crafting.Source.UIElements
{
    using System;
    using Godot;

    public partial class ActionButton : Button
    {
        private Action? _pressed;

        public void SetupNormalButton(string name, Action action,FocusModeEnum focus = FocusModeEnum.None)
        {
            _pressed = action;
            Pressed += () =>
            {
                _pressed?.Invoke();
            };
            Text = name;
            FocusMode = focus;
        }

        public void SetupToggleButton(string name, Action<bool> toggleAction, ButtonGroup? group = default, FocusModeEnum focus = FocusModeEnum.None)
        {
            ToggleMode = true;
            Toggled += pressed => toggleAction(pressed);
            Text = name;
            FocusMode = focus;
            if (group != null) ButtonGroup = group;
        }

        public void UpdateButtonState(bool isActive) => Disabled = !isActive;

        public override void _ExitTree()
        {
            _pressed = null;
        }
    }
}
