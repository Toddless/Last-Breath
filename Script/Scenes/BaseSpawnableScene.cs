namespace Playground.Script.Scenes
{
    using System.Collections.ObjectModel;
    using Playground.Script.Helpers;
    using Playground.Script.Enemy;
    using Godot;
    using System.ComponentModel;
    using System.Collections.Generic;

    public abstract partial class BaseSpawnableScene : ObservableNode2D
    {
        private ObservableCollection<EnemyAI>? _enemies = [];
        private RandomNumberGenerator _rnd = new();
        private EnemySpawner? _enemySpawner;
        private List<Vector2>? _enemiesRespawnPosition;

        public ObservableCollection<EnemyAI>? Enemies
        {
            get => _enemies;
            set => _enemies = value;
        }

        public EnemySpawner? EnemySpawner
        {
            get => _enemySpawner;
            set => _enemySpawner = value;
        }

        public RandomNumberGenerator Rnd
        {
            get => _rnd;
        }

        public List<Vector2>? EnemiesRespawnPosition
        {
            get => _enemiesRespawnPosition;
            set => _enemiesRespawnPosition = value;
        }

        public abstract void EnemiePropertyChanged(object? sender, PropertyChangedEventArgs e);
    }
}
