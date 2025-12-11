namespace Battle
{
    using Godot;
    using System;
    using Services;
    using Utilities;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;

    public partial class BattleHud : Control, IInitializable, IRequireServices
    {
        private const string UID = "uid://j6cqo6u0s83x";
        [Export] private Button? _returnButton;
        [Export] private TextureProgressBar? _playerHealthBar, _playerManaBar;
        [Export] private GridContainer? _playerEffects;
        [Export] private HBoxContainer? _stanceButtons, _abilityButtons;

        private IMediator _mediator = GameServiceProvider.Instance.GetService<IMediator>();

        public override void _Ready()
        {

        }

        public void InjectServices(IGameServiceProvider provider)
        {
            try
            {
                _mediator = provider.GetService<IMediator>();
            }
            catch (Exception ex)
            {
                Tracker.TrackError("Failed to inject services.", ex);
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
