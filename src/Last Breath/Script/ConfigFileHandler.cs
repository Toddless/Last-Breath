namespace Playground.Script
{
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Helpers;

    public partial class ConfigFileHandler : Node
    {
        private ConfigFile? _config = new();
        private const string ConfigFilePath = "res://Configuration/settings.ini";

        public override void _Ready()
        {
            if (!FileAccess.FileExists(ConfigFilePath))
            {
                _config?.SetValue(SettingsSection.Keybinging, "ui_left", "A");
                _config?.SetValue(SettingsSection.Keybinging, "ui_right", "D");
                _config?.SetValue(SettingsSection.Keybinging, "ui_up", "W");
                _config?.SetValue(SettingsSection.Keybinging, "ui_down", "S");
                _config?.SetValue(SettingsSection.Keybinging, "Inventory", "I");
                _config?.SetValue(SettingsSection.Keybinging, "Equip", "mouse_2");

                _config?.SetValue(SettingsSection.Video, SettingsParameter.Resolution, new Vector2I(1280, 720));
                _config?.SetValue(SettingsSection.Video, SettingsParameter.WindowMode, "Full-Screen");

                _config?.Save(ConfigFilePath);
            }
            else
            {
                _config?.Load(ConfigFilePath);
            }
        }

        public void SaveSettings(string section, string key, Variant value)
        {
            _config?.SetValue(section, key, value);
            _config?.Save(ConfigFilePath);
        }

        public Dictionary<string, Variant> LoadSettings(string section)
        {
            Dictionary<string, Variant> settings = [];
            // config cannot be null, because we create a new config when this class is initialized for the first time.
            foreach (var item in _config!.GetSectionKeys(section))
            {
                settings[item] = _config.GetValue(section, item);
            }

            return settings;
        }
    }
}
