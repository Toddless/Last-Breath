namespace Battle
{
    using Godot;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Mediator;
    using System.Collections.Generic;

    public partial class BattleHud : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://j6cqo6u0s83x";
        [Export] private Button? _returnButton;
        [Export] private TextureProgressBar? _playerHealthBar, _playerManaBar;
        [Export] private GridContainer? _playerEffects;
        [Export] private HBoxContainer? _stanceButtons, _abilityButtons;

        private IMediator? _mediator;

        public override void _Ready()
        {
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _mediator = provider.GetService<IMediator>();
        }

        public void SetFighterQueue(IEnumerable<IFightable> fighters)
        {
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
