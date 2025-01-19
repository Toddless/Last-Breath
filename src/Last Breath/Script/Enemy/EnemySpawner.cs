namespace Playground.Script.Enemy
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.Scenes;

    [Inject]
    public partial class EnemySpawner : Node, IEnemySpawner
    {
        private Dictionary<Vector2, EnemyAI?> _enemyPosition = new();
        private BaseSpawnableScene? _parentScene;
        private Queue<Action> _spawnQueue = new();
        private RandomNumberGenerator? _rnd;
        private bool _isProcessing = false;
        private PackedScene? _enemyToSpawn;
        private Timer? _timer;

        [Inject]
        protected RandomNumberGenerator? Rnd
        {
            get => _rnd;
            set => _rnd = value;
        }

        protected Queue<Action> SpawnQueue
        {
            get => _spawnQueue;
            set => _spawnQueue = value;
        }

        protected PackedScene? EnemyToSpawn
        {
            get => _enemyToSpawn;
            set => _enemyToSpawn = value;
        }

        protected BaseSpawnableScene? ParentScene
        {
            get => _parentScene;
            set => _parentScene = value;
        }

        protected Dictionary<Vector2, EnemyAI?> EnemyPositions
        {
            get => _enemyPosition;
            set => _enemyPosition = value;
        }

        public override void _Ready()
        {
            // TODO: When I am in a battle, enemies spawn in the battle scene
            ParentScene = (BaseSpawnableScene)GetParent();
            _timer = ParentScene.GetNode<Timer>($"{nameof(EnemySpawner)}/{nameof(Timer)}");
            EnemyToSpawn = ResourceLoader.Load<PackedScene>(ScenePath.EnemyToSpawn);
            ParentScene.Enemies!.CollectionChanged += OnCollectionChanged;
            ResolveDependencies();
        }

        private void ResolveDependencies() => DiContainer.InjectDependencies(this);

        public void InitializeEnemiesPositions(List<Vector2> positions)
        {
            foreach (Vector2 position in positions)
            {
                EnemyPositions[position] = null;
            }
        }

        public void DeleteEnemy(EnemyAI enemyToDelete)
        {
            var positionToFree = EnemyPositions!.FirstOrDefault(x => x.Value == enemyToDelete);
            EnemyPositions![positionToFree.Key] = null;
        }

        public void SpawnNewEnemy()
        {
            var freePosition = EnemyPositions!.FirstOrDefault(x => x.Value == null);
            EnemyAI enemy = EnemyToSpawn!.Instantiate<EnemyAI>();
            ParentScene!.CallDeferred("add_child", enemy);
            ParentScene.Enemies!.Add(enemy);
            enemy.GetNode<Area2D>("Area2D").BodyEntered += enemy.PlayerEntered;
            enemy.GetNode<Area2D>("Area2D").BodyExited += enemy.PlayerExited;
            enemy.PropertyChanged += ParentScene.EnemiePropertyChanged;
            enemy.Position = freePosition.Key;
            EnemyPositions![freePosition.Key] = enemy;
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

        private void EnqueueSpawnEnemyEvent()
        {
            SpawnQueue.Enqueue(SpawnNewEnemy);

            ProcessingQueue();
        }

        /// <summary>
        /// Instead of instant spawn an enemy, all spawn event will be saved to the queue
        /// </summary>
        private async void ProcessingQueue()
        {
            if (_isProcessing || SpawnQueue.Count == 0)
            {
                return;
            }
            _isProcessing = true;
            _timer!.Start(Rnd!.RandiRange(5, 15));
            if (_timer!.TimeLeft != 0)
            {
                await ToSignal(_timer, "timeout");
            }
            SpawnQueue.Dequeue().Invoke();

            _isProcessing = false;
        }
    }
}
