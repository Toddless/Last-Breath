namespace Crafting.Source.UIElements
{
    using System;
    using Godot;

    public partial class ActionButton : Button
    {
        private Action? _pressed;

        public void SetupNormalButton(string name, Action action, bool isActive = true, FocusModeEnum focus = FocusModeEnum.None)
        {
            _pressed = action;
            Pressed += _pressed;
            Text = name;
            Disabled = !isActive;
            FocusMode = focus;
        }

        public void SetupToggleButton(string name, Action<bool> action, ButtonGroup? group = default, FocusModeEnum focus = FocusModeEnum.None)
        {
            ToggleMode = true;
            Toggled += pressed => action(pressed);
            Text = name;
            FocusMode = focus;
            if(group != null) ButtonGroup = group;
        }


        public override void _ExitTree()
        {
            if (_pressed != null) Pressed -= _pressed;
            GD.Print($"Action button about to be free");
            _pressed = null;
        }
    }
}
