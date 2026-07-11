namespace Battle.Internal.Save
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
    internal partial class SaveLoadWindow : Control, IWindow
    {
        private const string ScenePath = "uid://cua8akmbr326r";

        [Export] private VBoxContainer? _slotsContainer;
        [Export] private Button? _closeButton;

        private ISaveGameService? _saveGame;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            if (_closeButton != null) _closeButton.Pressed += Close;
            Rebuild();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _saveGame = provider.GetService<ISaveGameService>();
        }

        public void Close() => GetParent().RemoveChild(this);

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(ScenePath);

        private void Rebuild()
        {
            if (_slotsContainer == null || _saveGame == null) return;

            foreach (var child in _slotsContainer.GetChildren())
                child.QueueFree();

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
            row.AddChild(SlotButton(TranslationServer.Translate("UI_Save"), !_saveGame.CanSave, () =>
            {
                _saveGame.SaveToSlot(slot);
                Rebuild();
            }));
            row.AddChild(SlotButton(TranslationServer.Translate("UI_Load"), metadata == null, () => _saveGame.RequestLoad(slot)));
            row.AddChild(SlotButton("X", metadata == null, () =>
            {
                _saveGame.DeleteSlot(slot);
                Rebuild();
            }));
            return row;
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
