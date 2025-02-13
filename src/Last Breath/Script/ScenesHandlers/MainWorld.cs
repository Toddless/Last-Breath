namespace Playground
{
    using Playground.Script;
    using System.ComponentModel;
    using Playground.Script.Enemy;
    using Playground.Script.Scenes;
    using System.Linq;

    public partial class MainWorld : BaseSpawnableScene
    {
        private BattleContext? _fight;
        private bool _isBattleActive;
        public BattleContext? Fight
        {
            get => _fight;
            set => SetProperty(ref _fight, value);
        }

        public override void _Ready()
        {
            EnemySpawner = GetNode<IEnemySpawner>(nameof(EnemySpawner));
            InitializeEnemies();
        }

        private void InitializeEnemies()
        {
            EnemiesRespawnPosition =
                [
                    new(259, 566),
                    new(728, 624),
                    new(878, 306),
                    new(515, 172),
                    new(290, 210),
                ];
            EnemySpawner?.InitializeEnemiesPositions(EnemiesRespawnPosition);

            for (int i = 0; i < EnemiesRespawnPosition.Count; i++)
            {
                EnemySpawner?.SpawnNewEnemy();
            }
        }

        public override void EnemyReadyToFight(object? sender, PropertyChangedEventArgs e)
        {
            if (_isBattleActive) return;
            if (e.PropertyName == nameof(BaseEnemy.PlayerEncounter))
            {
                var enemy = Enemies!.FirstOrDefault(x => x.PlayerEncounter == true);
                if (enemy != null)
                {
                    _isBattleActive = true;
                    Fight = new BattleContext(enemy, GameManager.Instance.Player!);
                }
            }
        }

        public void ResetBattleState() => _isBattleActive = false;

        protected override void ResolveDependencies() => DiContainer.InjectDependencies(this);
    }
}
