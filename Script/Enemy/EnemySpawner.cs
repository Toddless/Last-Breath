namespace Playground
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using Godot;

    public partial class EnemySpawner : Node
    {
        private RandomNumberGenerator _rnd = new();
        private MainScene? _parentScene;
        private PackedScene? _scene;
        private Queue<Action> _spawnQueue = new();
        private Timer? _timer;
        private bool _isProcessing = false;

        public override void _Ready()
        {
            _parentScene = (MainScene)GetParent();
            _scene = ResourceLoader.Load<PackedScene>("res://Node/Enemy.tscn");
            _timer = _parentScene.GetNode<Timer>($"{nameof(EnemySpawner)}/{nameof(Timer)}");
            _parentScene.Enemies!.CollectionChanged += OnCollectionChanged;
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
            EnemyAI enemy = _scene!.Instantiate<EnemyAI>();
            _parentScene!.CallDeferred("add_child", enemy);
            _parentScene.Enemies!.Add(enemy);
            enemy.Position = new Vector2(_rnd.RandfRange(50, 900), _rnd.RandfRange(250, 700));
            enemy.GetNode<Area2D>("Area2D").BodyEntered += enemy.PlayerEntered;
            enemy.GetNode<Area2D>("Area2D").BodyExited += enemy.PlayerExited;
            enemy.PropertyChanged += _parentScene.EnemiePropertyChanged;
        }
    }
}
