namespace Playground.Components
{
    using Godot;
    using Playground.Script;
    using Playground.Script.Passives.Attacks;

    // TODO: Need to rework completely
    [Inject]
    public class BattleBehavior
    {
        private RandomNumberGenerator? _rnd;
        private BaseEnemy? _enemyBase;
        private Player? _player;
        private ICharacter? _selfTarget;
        private ICharacter? _opponentTarget;

        public BattleBehavior(BaseEnemy enemy)
        {
            _enemyBase = enemy;
            _selfTarget = enemy;
            DiContainer.InjectDependencies(this);
        }

        [Inject]
        protected RandomNumberGenerator? Rnd
        {
            get => _rnd;
            set => _rnd = value;
        }

        public void GatherInfo(Player player)
        {
            _player = player;
            _opponentTarget = player;
        }

        public void BattleEnds()
        {
            _enemyBase = null;
        }
    }
}
