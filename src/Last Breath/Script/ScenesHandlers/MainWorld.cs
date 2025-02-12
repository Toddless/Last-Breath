namespace Playground
{
    using Playground.Script;
    using System.ComponentModel;
    using Playground.Script.Enemy;
    using Playground.Script.Scenes;

    public partial class MainWorld : BaseSpawnableScene
    {
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

        public override void EnemyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
           // PlayerInteracted(Enemies!.FirstOrDefault(x => x.PlayerEncounter == true));
        }

        protected override void ResolveDependencies() => DiContainer.InjectDependencies(this);
    }
}
