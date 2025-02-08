namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class WindowMode : BaseControl
    {
        private OptionButton? _options;
        private readonly string[] _windowMods =
        {
            "Full-Screen",
            "Window Mode",
            "Borderless Window",
            "Borderless Full-Screen"
        };

        public override void _Ready()
        {
            _options = GetNode<HBoxContainer>(nameof(HBoxContainer)).GetNode<OptionButton>(nameof(OptionButton));
            AddOptionItems();
            _options.ItemSelected += ItemSelected;

            Configuration?.LoadSettings(SettingsSection.Video);
        }

        protected override void AddOptionItems()
        {
            foreach (var mode in _windowMods)
            {
                _options?.AddItem(mode);
            }
        }

        private void ItemSelected(long index)
        {
            Configuration?.SaveSettings(SettingsSection.Video, SettingsParameter.Resolution, _windowMods[index]);
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
    }
}
