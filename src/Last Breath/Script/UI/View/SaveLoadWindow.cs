namespace LastBreath.Script.UI
{
    using Godot;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;

    public partial class SaveLoadWindow : Window, IInitializable, IRequireServices
    {
        private const string UID = "uid://cserxppd6wiui";
        [Export] private LocalizableButton? _returnButton, _loadBtn, _deleteBtn;

        private IUiMediator? _uiMediator;

        public override void _Ready()
        {
            _returnButton.Pressed += RaiseClose;

        }

        public override void _ExitTree()
        {
            if (_uiMediator != null) _uiMediator.UpdateUi -= UpdateUI;
        }

        public override void InjectServices(IGameServiceProvider provider)
        {
            _uiMediator = provider.GetService<IUiMediator>();
            _uiMediator.UpdateUi += UpdateUI;
        }

        private void UpdateUI()
        {
            _returnButton?.UpdateButtonText();
            _loadBtn?.UpdateButtonText();
            _deleteBtn?.UpdateButtonText();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
