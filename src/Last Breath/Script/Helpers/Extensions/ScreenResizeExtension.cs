namespace Playground.Script.Helpers.Extensions
{
    using Godot;
    public static class ScreenResizeExtension
    {
        public static void CenterWindow()
        {
            if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen) return;
            var screen = DisplayServer.WindowGetCurrentScreen();
            Vector2I screenPosition = DisplayServer.ScreenGetPosition(screen);
            Vector2I screenSize = DisplayServer.ScreenGetSize(screen);
            Vector2I windowSize = DisplayServer.WindowGetSize();

            Vector2I position = screenPosition + (screenSize - windowSize) / 2;
            DisplayServer.WindowSetPosition(position);
        }
    }
}
