namespace Core.Views.UI
{
    using Godot;

    public interface IUIWindowPositionStorage
    {
        Vector2? GetPosition<T>() where T : Control;
        void SavePosition<T>(Vector2 position) where T : Control;
    }
}
