namespace Playground.Components
{
    using Playground.Script;
    using Playground.Script.Passives.Attacks;

    // TODO: Need to rework completely
    public class BattleBehavior
    {
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
