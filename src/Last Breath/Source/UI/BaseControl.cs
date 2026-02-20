namespace LastBreath.Source.UI
{
    using Godot;
    using Script;

    public partial class BaseControl : Control
    {
        private SettingsHandler? _configFileHandler;

        protected SettingsHandler? Configuration
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
    }
}
