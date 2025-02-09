namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class SettingsTab : BaseControl
    {
        private VideoSettings? _videoSettings;
        private SoundSettings? _soundSettings;
        private TabBar? _videoTabBar, _soundTabBar, _uiTabBar;

        public override void _Ready()
        {
            Configuration = GetNode<ConfigFileHandler>(NodePathHelper.ConfigFileHandler);
            var root = GetNode<TabContainer>(nameof(TabContainer));
            // dont like it, maybe i will find a better solution
            var optionsMenu = GetOwner() as OptionsMenu;

            _videoTabBar = root.GetNode<TabBar>("Video");
            _uiTabBar = root.GetNode<TabBar>("UI");
            _soundTabBar = root.GetNode<TabBar>("Sound");

            _videoSettings = new(
                GetUIElement<OptionButton>(_videoTabBar, "HBoxContainerWindowMode", nameof(OptionButton)),
                GetUIElement<OptionButton>(_videoTabBar, "HBoxContainerResolution", nameof(OptionButton)));

            _soundSettings = new(
                GetUIElement<HSlider>(_soundTabBar, "HBoxContainerMaster", nameof(HSlider)),
                GetUIElement<HSlider>(_soundTabBar, "HBoxContainerMusic", nameof(HSlider)),
                GetUIElement<HSlider>(_soundTabBar, "HBoxContainerSfx", nameof(HSlider)));


            optionsMenu!.SavePressed += SaveSettings;
            _videoSettings.AddWindowModes();
            _videoSettings.AddResolutions();
            LoadSettings();
        }

        protected override void LoadSettings()
        {
            if (Configuration == null) return;
            _soundSettings?.LoadSettings(Configuration);
            _videoSettings?.LoadSettings(Configuration);
        }

        protected override void SaveSettings()
        {
            if (Configuration == null) return;
            _videoSettings?.SaveSettings(Configuration);
            _soundSettings?.SaveSettings(Configuration);
        }
    }
}
