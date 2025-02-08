namespace Playground.Script.UI
{
    using Godot;
    public partial class BaseControl : Control
    {
        private ConfigFileHandler? _configFileHandler;

        protected ConfigFileHandler? Configuration
        {
            get => _configFileHandler;
        }

        public override void _Ready() => _configFileHandler = GetNode<ConfigFileHandler>(nameof(ConfigFileHandler));


        protected virtual void AddOptionItems()
        {

        }

        protected virtual void SaveSettings()
        {

        }
    }
}
