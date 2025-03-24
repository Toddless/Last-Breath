namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class SettingsTab : BaseControl
    {
        private VideoSettings? _videoSettings;
        private SoundSettings? _soundSettings;
        private UISettings? _uiSettings;
        private TabBar? _videoTabBar, _soundTabBar, _uiTabBar;

        public override void _Ready()
        {
            Configuration = GetNode<ConfigFileHandler>(SingletonNodes.ConfigFileHandler);
            // dont like it, maybe i will find a better solution
            var optionsMenu = GetOwner() as OptionsMenu;

            _videoTabBar = (TabBar?)NodeFinder.FindBFSCached(this, "Video");
            _uiTabBar = (TabBar?)NodeFinder.FindBFSCached(this, "UI");
            _soundTabBar = (TabBar?)NodeFinder.FindBFSCached(this, "Sound");

            _videoSettings = new(
                (OptionButton?)NodeFinder.FindBFSCached(this, "OptionButtonWindowMode"),
                (OptionButton?)NodeFinder.FindBFSCached(this, "OptionButtonResolution"));
            _soundSettings = new(
                (HSlider?)NodeFinder.FindBFSCached(this, "HSliderMusic"),
                (HSlider?)NodeFinder.FindBFSCached(this, "HSliderSfx"),
                (HSlider?)NodeFinder.FindBFSCached(this, "HSliderMaster"));
            _uiSettings = new((OptionButton?)NodeFinder.FindBFSCached(this, "OptionButtonLanguage"));
            NodeFinder.ClearCache();

            optionsMenu!.SavePressed += SaveSettings;
            _videoSettings.AddWindowMods();
            _videoSettings.AddResolutions();
            _uiSettings.SetLanguages();
            LoadSettings();
        }

        protected override void LoadSettings()
        {
            if (Configuration == null) return;
            _soundSettings?.LoadSettings(Configuration);
            _videoSettings?.LoadSettings(Configuration);
            _uiSettings?.LoadSettings(Configuration);
        }

        protected override void SaveSettings()
        {
            if (Configuration == null) return;
            _videoSettings?.SaveSettings(Configuration);
            _soundSettings?.SaveSettings(Configuration);
            _uiSettings?.SaveSettings(Configuration);
        }
    }
}
