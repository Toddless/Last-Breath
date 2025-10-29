namespace LastBreath.Script.UI
{
    using Godot;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;

    public partial class SaveLoadWindow : Window, IInitializable, IRequireServices
    {
        private const string UID = "uid://cserxppd6wiui";
        [Export] private Button? _returnButton;

        public override void _Ready()
        {
            _returnButton.Pressed += RaiseClose;
        }


        public override void InjectServices(IGameServiceProvider provider)
        {

        }


        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
