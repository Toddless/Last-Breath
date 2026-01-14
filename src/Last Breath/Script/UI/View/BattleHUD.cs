namespace LastBreath
{
    using Godot;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using LastBreath.Script.UI;

    public partial class BattleHUD : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://j6cqo6u0s83x";

        [Export] private Button? _returnButton;
        [Export] private TextureProgressBar? _playerHealthBar, _enemyHealthBar;
        [Export] private GridContainer? _playerEffects, _enemyEffects;
        [Export] private TextureButton? _dexterityStance, _strengthStance, _intelligenceStance, _head, _body, _legs;
        [Export] private ResourceProgressBar? _playerResource, _enemyResource;
        [Export] private TextureButton? _player, _enemy;
        [Export] private AbilityButtons? _abilities;
        [Export] private HBoxContainer? _attackButtons;
        [Export] private VBoxContainer? _stanceContainer, _bodyContainer;


        public override void _Ready()
        {

        }

        public void InjectServices(IGameServiceProvider provider)
        {

        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
