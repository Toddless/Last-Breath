namespace Playground.Script.Helpers.Extensions
{
    using Godot;
    public static class ScreenResizeExtension
    {
        public static void CenterWindow()
        {
            if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen) return;
            var screen = DisplayServer.WindowGetCurrentScreen();
            DisplayServer.WindowSetPosition(DisplayServer.ScreenGetPosition(screen) + (DisplayServer.ScreenGetSize(screen) - DisplayServer.WindowGetSize()) / 2);
        }
    }
}
