namespace LastBreath.Script.UI
{
    using Godot;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;

    public partial class PlayerHUD : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://boqqyrt0sfpve";

        [Export] private Button? _characterBtn, _inventoryBtn, _questsBtn;
        [Export] private TextureProgressBar? _playerHealth;
        [Export] private GridContainer? _playerEffects;


        public override void _Ready()
        {

        }

        public void InjectServices(IGameServiceProvider provider)
        {

        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
