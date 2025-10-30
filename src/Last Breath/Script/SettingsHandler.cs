namespace LastBreath.Script
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Mediator;
    using LastBreath.Script.Helpers;

    public class SettingsHandler : ISettingsHandler
    {
        private readonly ConfigFile _config = new();
        private const string ConfigFilePath = "res://Configuration/settings.ini";
        private readonly string[] _languages = ["en", "ru"];
        private readonly Vector2I[] _resolution = [new(1366, 768), new(1920, 1080), new(2560, 1440)];
        private readonly string[] _windowMods = ["Full-Screen", "Window", "Borderless Window"];
        private readonly string[] _resolutins = ["1366 x 768", "1920 x 1080", "2560 x 1440"];
        private readonly IUiMediator _uiMediator;

        public SettingsHandler(IUiMediator uiMediator)
        {
            if (!FileAccess.FileExists(ConfigFilePath))
            {
                _config.SetValue(SettingsSection.Keybinging, "Equip", "mouse_2");
                _config.SetValue(SettingsSection.Keybinging, Settings.MoveLeft, "A");
                _config.SetValue(SettingsSection.Keybinging, Settings.MoveRight, "D");
                _config.SetValue(SettingsSection.Keybinging, Settings.MoveUp, "W");
                _config.SetValue(SettingsSection.Keybinging, Settings.MoveDown, "S");
                _config.SetValue(SettingsSection.Keybinging, Settings.Inventory, "I");

                _config.SetValue(SettingsSection.Video, Settings.Resolution, 0);
                _config.SetValue(SettingsSection.Video, Settings.WindowMode, 1);
                _config.SetValue(SettingsSection.Video, Settings.Borderless, false);
                _config.SetValue(SettingsSection.Sound, Settings.Master, 0.1);
                _config.SetValue(SettingsSection.Sound, Settings.Music, 0.1);
                _config.SetValue(SettingsSection.Sound, Settings.Sfx, 0.1);
                _config.SetValue(SettingsSection.UI, Settings.Language, 0);
                _config.Save(ConfigFilePath);
            }
            else
                _config.Load(ConfigFilePath);

            _uiMediator = uiMediator;
        }

        public void ApplySavedSettings()
        {
            LoadSoundSettings();
            LoadVideoSettings();
            LoadUISettings();
            _uiMediator.RaiseUpdateUi();
        }

        public string[] GetWindowMods() => _windowMods;
        public string[] GetWindowResolutions() => _resolutins;
        public string[] GetLanguages() => _languages;
        public Variant GetSettingValue(string section, string setting)=> _config.GetValue(section, setting);
        public void SetResolution(long index)
        {
            // changing resolution in Full screen mode does nothing, for now.
            if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Fullscreen)
                DisplayServer.WindowSetSize(_resolution[index]);

            var result = SaveSettings(SettingsSection.Video, Settings.Resolution, index);
            if (result != Error.Ok)
                Tracker.TrackError($"Failed to save Video setting. Error: {result}", this);
        }

        public void SetWindowMode(long index, bool borderless = false)
        {
            switch (index)
            {
                case 0:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                    SetBorderlessFlag(borderless);
                    break;
                case 1:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                    SetBorderlessFlag(borderless);
                    break;
                case 2:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                    SetBorderlessFlag(borderless);
                    break;
                default:
                    break;
            }
        }

        public void SetLanguage(long index)
        {
            TranslationServer.SetLocale(_languages[(int)index]);
            var result = SaveSettings(SettingsSection.UI, Settings.Language, index);
            if (result != Error.Ok)
                Tracker.TrackError($"Failed to save Language setting. Error: {result}", this);
        }

        public void SetSoundBus(SoundBus bus, double value)
        {
            AudioServer.SetBusVolumeDb((int)bus, (float)Mathf.LinearToDb(value));
            var result = SaveSettings(SettingsSection.Sound, bus.ToString(), value);
            if (result != Error.Ok)
                Tracker.TrackError($"Failed to save Sound setting. Bus: {bus}, Error: {result}", this);
        }


        private Error SaveSettings(string section, string key, Variant value)
        {
            _config.SetValue(section, key, value);
            return _config.Save(ConfigFilePath);
        }

        private void SetBorderlessFlag(bool flag) => DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, flag);

        private void LoadSoundSettings()
        {
            foreach (var bus in Enum.GetValues<SoundBus>())
            {
                var value = (float)_config.GetValue(SettingsSection.Sound, bus.ToString());
                AudioServer.SetBusVolumeDb((int)bus, Mathf.LinearToDb(value));
            }
        }

        private void LoadVideoSettings()
        {
            var mode = (long)_config.GetValue(SettingsSection.Video, Settings.WindowMode);
            var resolution = (int)_config.GetValue(SettingsSection.Video, Settings.Resolution);
            var borderless = (bool)_config.GetValue(SettingsSection.Video, Settings.Borderless);
            DisplayServer.WindowSetSize(_resolution[resolution]);
            SetWindowMode(mode, borderless);

        }

        private void LoadUISettings()
        {
            TranslationServer.SetLocale(_languages[(int)_config.GetValue(SettingsSection.UI, Settings.Language)]);
        }
    }
}
