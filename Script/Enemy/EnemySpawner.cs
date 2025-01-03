namespace Playground.Script.Enemy
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Godot;
    using Playground.Script.Scenes;

    public partial class EnemySpawner : Node
    {
        private Dictionary<Vector2, EnemyAI?> _enemyPosition = new();
        private RandomNumberGenerator _rnd = new();
        private Queue<Action> _spawnQueue = new();
        private bool _isProcessing = false;
        private BaseSpawnableScene? _parentScene;
        private PackedScene? _enemyToSpawn;
        private Timer? _timer;

        public override void _Ready()
        {

            // TODO: Enemy spawn on battle scene to if i am in battle
            _parentScene = (BaseSpawnableScene)GetParent();
            _timer = _parentScene.GetNode<Timer>($"{nameof(EnemySpawner)}/{nameof(Timer)}");
            _parentScene.Enemies!.CollectionChanged += OnCollectionChanged;
            _enemyToSpawn =ResourceLoader.Load<PackedScene>("res://Scenes/Enemy.tscn");

            GD.Print($"Scene initialized: {this.Name}");

        }

        /// <summary>
        /// i need to spawn an enemy only if enemy was killed and removed from list, thats why i have here if-statement
        /// </summary>
        /// <param name="sender">To be added</param>
        /// <param name="e">To be added</param>
        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action is not NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            EnqueueSpawnEnemyEvent();
        }

        public void InitializeEnemiesPositions(List<Vector2> positions)
        {
            foreach (Vector2 position in positions)
            {
                _enemyPosition[position] = null;
            }
        }

        public void DeleteEnemy(EnemyAI enemyToDelete)
        {
            var positionToFree = _enemyPosition!.FirstOrDefault(x => x.Value == enemyToDelete);
            _enemyPosition![positionToFree.Key] = null;
        }

        private void EnqueueSpawnEnemyEvent()
        {
            _spawnQueue.Enqueue(SpawnNewEnemy);

            ProcessingQueue();
        }

        /// <summary>
        /// Instead of instant spawn an enemy, all spawn event will be saved to the queue
        /// </summary>
        private async void ProcessingQueue()
        {
            if (_isProcessing || _spawnQueue.Count == 0)
            {
                return;
            }
            _isProcessing = true;
            _timer!.Start(_rnd.RandiRange(5, 15));
            if (_timer!.TimeLeft != 0)
            {
                await ToSignal(_timer, "timeout");
            }
            _spawnQueue.Dequeue();

            _isProcessing = false;
        }

        public void SpawnNewEnemy()
        {
            var freePosition = _enemyPosition!.FirstOrDefault(x => x.Value == null);
            EnemyAI enemy = _enemyToSpawn!.Instantiate<EnemyAI>();
            _parentScene!.CallDeferred("add_child", enemy);
            _parentScene.Enemies!.Add(enemy);
            enemy.GetNode<Area2D>("Area2D").BodyEntered += enemy.PlayerEntered;
            enemy.GetNode<Area2D>("Area2D").BodyExited += enemy.PlayerExited;
            enemy.PropertyChanged += _parentScene.EnemiePropertyChanged;
            enemy.Position = freePosition.Key;
            _enemyPosition![freePosition.Key] = enemy;

            GD.Print($"{enemy.VisibilityLayer}, {enemy.CollisionLayer}");
        }
    }
}
