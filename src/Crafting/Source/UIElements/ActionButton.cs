namespace Crafting.Source.UIElements
{
    using System;
    using Godot;

    public partial class ActionButton : Button
    {
        private Action? _pressed;
        private Func<bool>? _isActive;

        public void SetupNormalButton(string name, Action action, Func<bool> isActive, FocusModeEnum focus = FocusModeEnum.None)
        {
            _isActive = isActive;
            _pressed = action;
            Pressed += () =>
            {
                _pressed?.Invoke();
                Disabled = _isActive == null || !_isActive.Invoke();
            };
            Text = name;
            Disabled = !_isActive.Invoke();
            FocusMode = focus;
        }

        public void SetupToggleButton(string name, Action<bool> action, ButtonGroup? group = default, FocusModeEnum focus = FocusModeEnum.None)
        {
            ToggleMode = true;
            Toggled += pressed => action(pressed);
            Text = name;
            FocusMode = focus;
            if (group != null) ButtonGroup = group;
        }

        public void UpdateButtonState(bool isActive) => Disabled = !isActive;

        public override void _ExitTree()
        {
            _pressed = null;
            _isActive = null;
        }
    }
}
