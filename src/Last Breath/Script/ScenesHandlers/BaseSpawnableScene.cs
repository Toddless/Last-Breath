namespace Playground.Script.Scenes
{
    using System.Collections.ObjectModel;
    using Playground.Script.Helpers;
    using Playground.Script.Enemy;
    using Godot;
    using System.ComponentModel;
    using System.Collections.Generic;

    [Inject]
    public abstract partial class BaseSpawnableScene : ObservableNode2D
    {
        private ObservableCollection<BaseEnemy>? _enemies = [];
        private RandomNumberGenerator? _rnd;
        private IEnemySpawner? _enemySpawner;
        private List<Vector2>? _enemiesRespawnPosition;

        public ObservableCollection<BaseEnemy>? Enemies
        {
            get => _enemies;
            set => SetProperty(ref _enemies, value);
        }

        public IEnemySpawner? EnemySpawner
        {
            get => _enemySpawner;
            set => _enemySpawner = value;
        }

        [Inject]
        protected RandomNumberGenerator? Rnd
        {
            get => _rnd;
            set => _rnd = value;
        }

        public List<Vector2>? EnemiesRespawnPosition
        {
            get => _enemiesRespawnPosition;
            set => _enemiesRespawnPosition = value;
        }

        protected abstract void ResolveDependencies();

        public abstract void EnemyPropertyChanged(object? sender, PropertyChangedEventArgs e);
    }
}
