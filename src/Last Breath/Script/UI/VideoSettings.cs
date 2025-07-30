namespace LastBreath.Script.UI
{
    using System;
    using Godot;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.Helpers.Extensions;

    public class VideoSettings : ISettings
    {
        private readonly OptionButton _windowModeOptions, _resolutionOptions;
        private const string InitialResolution = "1366 x 768";
        private const string FullHD = "1920 x 1080";
        private const string QuadHD = "2560 x 1440";
        private const string FullScreen = "Full-Screen";
        private const string Window = "Window";
        private const string BorderlessWindow = "Borderless Window";

        private readonly Vector2I[] _resolution =
        [
            new(1366, 768),
            new(1920, 1080),
            new(2560, 1440)
        ];

        public VideoSettings(OptionButton? windowMode, OptionButton? resolution)
        {
            ArgumentNullException.ThrowIfNull(windowMode);
            ArgumentNullException.ThrowIfNull(resolution);
            _windowModeOptions = windowMode;
            _resolutionOptions = resolution;
            _windowModeOptions.ItemSelected += WindowModeItemSelected;
            _resolutionOptions.ItemSelected += ResolutionItemSelected;
        }

        public void AddResolutions()
        {
            _resolutionOptions?.AddItem(InitialResolution);
            _resolutionOptions?.AddItem(FullHD);
            _resolutionOptions?.AddItem(QuadHD);

        }

        public void AddWindowMods()
        {
            _windowModeOptions?.AddItem(FullScreen);
            _windowModeOptions?.AddItem(Window);
            _windowModeOptions?.AddItem(BorderlessWindow);
        }

        public void LoadSettings(ConfigFileHandler config)
        {
            ResolutionItemSelected(_resolutionOptions.Selected = (int)config.LoadSetting(SettingsSection.Video, Settings.Resolution));
            WindowModeItemSelected(_windowModeOptions.Selected = (int)config.LoadSetting(SettingsSection.Video, Settings.WindowMode));
        }

        public void SaveSettings(ConfigFileHandler config)
        {
            config.SaveSettings(SettingsSection.Video, Settings.WindowMode, _windowModeOptions.Selected);
            config.SaveSettings(SettingsSection.Video, Settings.Resolution, _resolutionOptions.Selected);
        }

        private void ResolutionItemSelected(long index)
        {
            // changing resolution in Full screen mode does nothing, for now.
            // need more info about scaling.
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
