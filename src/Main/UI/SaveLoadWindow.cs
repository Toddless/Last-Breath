namespace LastBreath.UI
{
    using Core.Data;
    using Core.Save;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// Checkpoint save/load screen: 10 slots with metadata (location, mastery level, date),
    /// save/load/delete per slot. The rows are data-driven and built in code; the scene only
    /// hosts the frame — restyle freely later.
    /// </summary>
    [GlobalClass]
    public partial class SaveLoadWindow : Control, IWindow
    {
        private const string ScenePath = "uid://cserxppd6wiui";

        [Export] private VBoxContainer? _slotsContainer;
        [Export] private SharedUi.WindowHeader? _header;

        private ISaveGameService? _saveGame;
        private System.Action? _menuLoad;


        /// <summary>
        /// Menu mode: there is no live world to reload, so a successful load stages the pending
        /// file and hands the scene switch to the host (the main menu owns the world scene).
        /// </summary>
        public void SetMenuLoadHandler(System.Action switchToWorld) => _menuLoad = switchToWorld;

        public override void _Ready()
        {
            if (_header != null)
            {
                _header.SetTitle(Core.Localization.Localization.Localize("SaveLoadLabel"));
                _header.Closed += Close;
            }
            Rebuild();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _saveGame = provider.GetService<ISaveGameService>();
        }

        // Closing means dying (IWindow contract): a detached-but-alive window stays tracked by
        // the manager, and the next Toggle calls Close on a parentless node — the reopen NRE.
        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath);

        private void Rebuild()
        {
            if (_slotsContainer == null || _saveGame == null) return;

            _slotsContainer.QueueFreeChildren();

            for (int slot = 1; slot <= _saveGame.SlotCount; slot++)
                _slotsContainer.AddChild(BuildRow(slot));
        }

        private Control BuildRow(int slot)
        {
            var metadata = _saveGame!.PeekSlot(slot);
            var row = new HBoxContainer();
            row.AddChild(new Label
            {
                Text = DescribeSlot(slot, metadata),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center
            });
            row.AddChild(SlotButton(Core.Localization.Localization.Localize("UI_Save"), !_saveGame.CanSave, () =>
            {
                _saveGame.SaveToSlot(slot);
                Rebuild();
            }));
            row.AddChild(SlotButton(Core.Localization.Localization.Localize("UI_Load"), metadata == null, () => LoadSlot(slot)));
            row.AddChild(SlotButton("X", metadata == null, () =>
            {
                _saveGame.DeleteSlot(slot);
                Rebuild();
            }));
            return row;
        }

        private void LoadSlot(int slot)
        {
            if (_menuLoad != null)
            {
                if (_saveGame!.StageLoad(slot)) _menuLoad();
                return;
            }

            _saveGame!.RequestLoad(slot); // in-game: the scene reloads, the SaveDirector applies
        }

        private static string DescribeSlot(int slot, SaveMetadata? metadata) =>
            metadata == null
                ? $"{slot}. —"
                : $"{slot}. {metadata.Location} · {metadata.MasteryLevel} · {metadata.SavedAtUtc.ToLocalTime():dd.MM HH:mm}";

        private static Button SlotButton(string text, bool disabled, System.Action onPressed)
        {
            var button = new Button { Text = text, Disabled = disabled };
            button.Pressed += onPressed;
            return button;
        }
    }
}
