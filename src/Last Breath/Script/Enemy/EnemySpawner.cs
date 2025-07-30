namespace LastBreath.Script.Enemy
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Godot;
    using LastBreath.Script.Helpers;

    // TODO: Need to rework this
    public partial class EnemySpawner : Node, IEnemySpawner
    {
        private Dictionary<Vector2, BaseEnemy?> _enemyPosition = new();
        private MainWorld? _parentScene;
        private Queue<Action> _spawnQueue = new();
        private RandomNumberGenerator? _rnd;
        private bool _isProcessing = false;
        private PackedScene? _enemyToSpawn;

        #region Properties
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

        protected MainWorld? ParentScene
        {
            get => _parentScene;
            set => _parentScene = value;
        }

        protected Dictionary<Vector2, BaseEnemy?> EnemyPositions
        {
            get => _enemyPosition;
            set => _enemyPosition = value;
        }
        #endregion

        public override void _Ready()
        {
            // TODO: When I am in a battle, enemies spawn in the battle scene
            ParentScene = (MainWorld)GetParent();
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

        public void DeleteEnemy(BaseEnemy enemyToDelete)
        {
            var positionToFree = EnemyPositions!.FirstOrDefault(x => x.Value == enemyToDelete);
            EnemyPositions![positionToFree.Key] = null;
        }

        public void SpawnNewEnemy()
        {
            var freePosition = EnemyPositions!.FirstOrDefault(x => x.Value == null);
            BaseEnemy enemy = EnemyToSpawn!.Instantiate<BaseEnemy>();
            ParentScene!.CallDeferred("add_child", enemy);
            ParentScene.Enemies!.Add(enemy);
            enemy.InitializeFight += ParentScene.InitializingFight;
            enemy.Position = freePosition.Key;
            EnemyPositions![freePosition.Key] = enemy;
        }

        /// <summary>
        /// i need to spawn an enemy only if enemy was killed and removed from list
        /// </summary>
        /// <param name="sender">To be added</param>
        /// <param name="e">To be added</param>
        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action is not NotifyCollectionChangedAction.Remove) return;
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
            var timer = GetTree().CreateTimer(Rnd!.RandiRange(5, 15));
            await ToSignal(timer, "timeout");
            SpawnQueue.Dequeue().Invoke();

            _isProcessing = false;
        }
    }
}
