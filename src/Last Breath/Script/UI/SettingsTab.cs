namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class SettingsTab : BaseControl
    {
        private OptionButton? _windowModeOptions, _resolutionOptions;
        private TabBar? _videoTabBar, _soundTabBar, _graphicsTabBar, _keybindingTabBar;

        private readonly string[] _windowMods =
        {
            "Full-Screen",
            "Window Mode",
            "Borderless Window",
            "Borderless Full-Screen"
        };

        // values to show in UI
        private readonly string[] _resolutions =
        {
             "1280 x 720",
             "1920 x 1080",
             "2560 x 1440"
        };
        // Actual values
        private readonly Vector2I[] _resolution =
        {
            new(1280, 720),
            new(1920, 1080),
            new(2560, 1440)
        };

        public override void _Ready()
        {
            Configuration = GetNode<ConfigFileHandler>(NodePathHelper.ConfigFileHandler);
            var root = GetNode<TabContainer>(nameof(TabContainer));
            var optionsMenu = GetOwner() as OptionsMenu;
            _soundTabBar = root.GetNode<TabBar>("Sound");
            _videoTabBar = root.GetNode<TabBar>("Video");
            _graphicsTabBar = root.GetNode<TabBar>("Graphics");
            _keybindingTabBar = root.GetNode<TabBar>("Keybindings");

            _windowModeOptions = GetPathToNode(_videoTabBar)?.GetNode<HBoxContainer>("HBoxContainerWindowMode").GetNode<OptionButton>(nameof(OptionButton));
            _resolutionOptions = GetPathToNode(_videoTabBar)?.GetNode<HBoxContainer>("HBoxContainerResolution").GetNode<OptionButton?>(nameof(OptionButton));
            optionsMenu!.SavePressed += SaveSettings;
            AddResolutions();
            AddWindowModes();
            SetEvents();
            SetLoadedSettings();
        }

        private void SetLoadedSettings()
        {
            if (Configuration == null) return;
            var savedWindowMode = (int)Configuration.LoadSetting(SettingsSection.Video, SettingsParameter.WindowMode);
            var savedResolution = (int)Configuration.LoadSetting(SettingsSection.Video, SettingsParameter.Resolution);
            _windowModeOptions!.Selected = savedWindowMode;
            _resolutionOptions!.Selected = savedResolution;
            ResolutionItemSelected(savedResolution);
            WindowModeItemSelected(savedWindowMode);
        }

        private void SetEvents()
        {
            _windowModeOptions!.ItemSelected += WindowModeItemSelected;
            _resolutionOptions!.ItemSelected += ResolutionItemSelected;
        }

        private VBoxContainer? GetPathToNode(TabBar parent)
        {
            return parent.GetNode<MarginContainer>(nameof(MarginContainer)).GetNode<ScrollContainer>(nameof(ScrollContainer)).GetNode<VBoxContainer>(nameof(VBoxContainer));
        }

        private void AddResolutions()
        {
            foreach (var resolution in _resolutions)
            {
                _resolutionOptions?.AddItem(resolution);
            }
        }

        private void AddWindowModes()
        {
            foreach (var mode in _windowMods)
            {
                _windowModeOptions?.AddItem(mode);
            }
        }

        private void ResolutionItemSelected(long index) => DisplayServer.WindowSetSize(_resolution[index]);

        private void WindowModeItemSelected(long index)
        {
            switch (index)
            {
                case 0:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                    DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
                    break;
                case 1:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                    DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
                    break;
                case 2:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                    DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
                    break;
                case 3:
                    DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                    DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
                    break;
                default:
                    break;
            }
        }


        // TODO: Save button
        protected override void SaveSettings()
        {
            Configuration?.SaveSettings(SettingsSection.Video, SettingsParameter.Resolution, _resolutionOptions!.Selected);
            Configuration?.SaveSettings(SettingsSection.Video, SettingsParameter.WindowMode, _windowModeOptions!.Selected);
        }
    }
}
