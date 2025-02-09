namespace Playground.Script.UI
{
    using Godot;

    public partial class BaseControl : Control
    {
        private ConfigFileHandler? _configFileHandler;

        protected ConfigFileHandler? Configuration
        {
            get => _configFileHandler;
            set => _configFileHandler = value;
        }

        protected virtual void SaveSettings()
        {

        }

        protected virtual void LoadSettings()
        {

        }

        protected T GetUIElement<T>(TabBar parent, string containerName, string elementType)
            where T : class
        {
            return parent.GetNode<MarginContainer>(nameof(MarginContainer))
                .GetNode<ScrollContainer>(nameof(ScrollContainer))
                .GetNode<VBoxContainer>(nameof(VBoxContainer))
                .GetNode<HBoxContainer>(containerName)
                .GetNode<T>(elementType);
        }
    }
}
