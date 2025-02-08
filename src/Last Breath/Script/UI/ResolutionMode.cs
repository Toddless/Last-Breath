namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class ResolutionMode : BaseControl
    {
        private OptionButton? _options;
        // TODO: i dont like it
        private readonly string[] _resolutions =
        {
             "1280 x 720",
             "1920 x 1080",
             "2560 x 1440"
        };
        // TODO: i dont like it
        private readonly Vector2I[] _resolution =
        {
            new(1280, 720),
            new(1920, 1080),
            new(2560, 1440),
        };

        public override void _Ready()
        {
            _options = GetNode<HBoxContainer>(nameof(HBoxContainer)).GetNode<OptionButton>(nameof(OptionButton));
            AddOptionItems();
            _options.ItemSelected += ItemSelected;
        }

        protected override void AddOptionItems()
        {
            foreach (var resolution in _resolutions)
            {
                _options?.AddItem(resolution);
            }
        }

        private void ItemSelected(long index)
        {
            Configuration?.SaveSettings(SettingsSection.Video,SettingsParameter.Resolution, _resolution[index]);
            DisplayServer.WindowSetSize(_resolution[index]);
        }
    }
}
