namespace Playground
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Godot;

    public partial class EnemySpawner : Node
    {
        private Dictionary<Vector2, EnemyAI?>? _enemyPosition = new();
        private RandomNumberGenerator _rnd = new();
        private Queue<Action> _spawnQueue = new();
        private bool _isProcessing = false;
        private MainScene? _parentScene;
        private PackedScene? _scene;
        private Timer? _timer;

        public override void _Ready()
        {

            // TODO: Enemy spawn on main scene too if i am in battle
            _parentScene = (MainScene)GetParent();
            _scene = ResourceLoader.Load<PackedScene>("res://Node/Enemy.tscn");
            _timer = _parentScene.GetNode<Timer>($"{nameof(EnemySpawner)}/{nameof(Timer)}");
            _parentScene.Enemies!.CollectionChanged += OnCollectionChanged;

            foreach (var respawnPosition in _parentScene.EnemiesRespawnPosition)
            {
                _enemyPosition![respawnPosition] = null;
            }
        }

        /// <summary>
        /// first: we need to spawn an enemy only if enemy was killed and removed from list
        /// second: if some timer is running, we await until timeout and then spawn new enemy
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
            while (_spawnQueue.Count > 0)
            {
                _isProcessing = true;
                var nextEvent = _spawnQueue.Dequeue();
                if (_timer!.TimeLeft != 0)
                {
                    await ToSignal(_timer, "timeout");
                }
                nextEvent.Invoke();
                _timer!.Start(_rnd.RandiRange(1, 15));
            }

            _isProcessing = false;
        }

        public void SpawnNewEnemy()
        {
            var freePosition = _enemyPosition!.FirstOrDefault(x => x.Value == null);
            EnemyAI enemy = _scene!.Instantiate<EnemyAI>();
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
