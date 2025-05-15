namespace Playground.Script.Helpers
{
    using Godot;
    using Playground.Script.Enums;

    public class MouseInputHelper
    {
        public static MouseButtonPressed GetPressedButtons(InputEventMouseButton pressed)
        {
            if (!pressed.Pressed) return MouseButtonPressed.None;

            if (pressed.AltPressed)
            {
                return pressed.ButtonIndex switch
                {
                    MouseButton.Left => MouseButtonPressed.AltLeftClick,
                    MouseButton.Right => MouseButtonPressed.AltRightClick,
                    _ => MouseButtonPressed.None,
                };
            }

            if (pressed.CtrlPressed)
            {
                return pressed.ButtonIndex switch
                {
                    MouseButton.Left => MouseButtonPressed.CtrLeftClick,
                    MouseButton.Right => MouseButtonPressed.CtrRightClick,
                    _ => MouseButtonPressed.None,
                };
            }

            return pressed.ButtonIndex switch
            {
                MouseButton.Left => MouseButtonPressed.LeftClick,
                MouseButton.Right => MouseButtonPressed.RightClick,
                _ => MouseButtonPressed.None,
            };
        }
    }
}
