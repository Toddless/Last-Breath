namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.Helpers.Extensions;

    public class VideoSettings : ISettings
    {
        private OptionButton _windowModeOptions, _resolutionOptions;

        private readonly Vector2I[] _resolution =
        [
            new(1280, 720),
            new(1920, 1080),
            new(2560, 1440)
        ];

        private readonly string[] _windowMods =
        [
            "Full-Screen",
            "Window",
            "Borderless Window",
        ];

        // values to show in UI
        private readonly string[] _resolutions =
        [
             "1280 x 720",
             "1920 x 1080",
             "2560 x 1440"
        ];

        public VideoSettings(OptionButton windowMode, OptionButton resolution)
        {
            _windowModeOptions = windowMode;
            _resolutionOptions = resolution;
            _windowModeOptions.ItemSelected += WindowModeItemSelected;
            _resolutionOptions.ItemSelected += ResolutionItemSelected;
        }

        public void AddResolutions()
        {
            foreach (var resolution in _resolutions)
            {
                _resolutionOptions?.AddItem(resolution);
            }
        }

        public void AddWindowModes()
        {
            foreach (var mode in _windowMods)
            {
                _windowModeOptions?.AddItem(mode);
            }
        }

        public void LoadSettings(ConfigFileHandler config)
        {
            ResolutionItemSelected(_resolutionOptions.Selected = (int)config.LoadSetting(SettingsSection.Video, SettingsParameter.Resolution));
            WindowModeItemSelected(_windowModeOptions.Selected = (int)config.LoadSetting(SettingsSection.Video, SettingsParameter.WindowMode));
        }

        public void SaveSettings(ConfigFileHandler config)
        {
            config.SaveSettings(SettingsSection.Video, SettingsParameter.WindowMode, _windowModeOptions.Selected);
            config.SaveSettings(SettingsSection.Video, SettingsParameter.Resolution, _resolutionOptions.Selected);
        }

        private void ResolutionItemSelected(long index)
        {
            if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Fullscreen)
                DisplayServer.WindowSetSize(_resolution[index]);
            ScreenResizeExtension.CenterWindow();
        }

        private void WindowModeItemSelected(long index)
        {
            // The game does not work correctly in the windowed mode.
            // Buttons such as Close or Minimize are not displayed.
            switch (index)
            {
                case 0:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                    SetBorderlessFlag(false);
                    break;
                case 1:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                    SetBorderlessFlag(false);
                    break;
                case 2:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                    SetBorderlessFlag(true);
                    break;
                default:
                    break;
            }
            ScreenResizeExtension.CenterWindow();
        }

        private void SetBorderlessFlag(bool flag) => DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, flag);

    }
}
