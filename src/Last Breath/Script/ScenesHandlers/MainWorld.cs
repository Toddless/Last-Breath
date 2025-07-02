namespace Playground
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Godot;
    using Playground.Script;
    using Playground.Script.Enemy;
    using Playground.Script.Helpers;
    using Playground.Script.NPC;
    using Playground.Script.ScenesHandlers;

    public partial class MainWorld : ObservableNode2D
    {
        private bool _isBattleActive;
        private readonly List<BaseNPC> _npcs = [];
        private readonly List<BaseOpenableObject> _openableObjects = [];
        private Area2D? _area2D;
        private ObservableCollection<BaseEnemy>? _enemies = [];
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

        public List<Vector2>? EnemiesRespawnPosition
        {
            get => _enemiesRespawnPosition;
            set => _enemiesRespawnPosition = value;
        }

        public List<BaseOpenableObject> OpenableObjects => _openableObjects;
        public List<BaseNPC> NPCs => _npcs;
        public event Action<string>? CutScene;

        public event Action<BattleContext>? InitializeFight;

        public override void _Ready()
        {
            _area2D = GetNode<Area2D>(nameof(Area2D));
            EnemySpawner = GetNode<IEnemySpawner>(nameof(EnemySpawner));
            _area2D.BodyEntered += OnEnterArea;
            ChildEnteredTree += OnChildAdded;
            AddNpcsToList();
            AddOpenableObjects();
            InitializeEnemies();
        }

        public void InitializingFight(ICharacter enemy)
        {
            if (_isBattleActive) return;
            InitializeFight?.Invoke(new BattleContext([enemy, GameManager.Instance.Player!]));
            _isBattleActive = true;
        }

        public void ResetBattleState() => _isBattleActive = false;

        protected void ResolveDependencies() => DiContainer.InjectDependencies(this);

        // if i need dynamically add new openableObjects
        private void OnChildAdded(Node node)
        {
            if (node is BaseOpenableObject obj)
            {
                _openableObjects.Add(obj);
                obj.ChildExitingTree += OnChildExiting;
            }
            if (node is BaseNPC npc)
            {
                _npcs.Add(npc);
            }
        }

        private void AddOpenableObjects()
        {
            _openableObjects.AddRange(GetChildren().OfType<BaseOpenableObject>().ToList());
            _openableObjects.ForEach(x => x.ChildExitingTree += OnChildExiting);
        }

        private void OnChildExiting(Node node)
        {
            if (node is BaseOpenableObject obj)
            {
                _openableObjects.Remove(obj);
                obj.ChildExitingTree -= OnChildExiting;
            }
        }

        private void AddNpcsToList() => _npcs.AddRange(GetChildren().OfType<BaseNPC>().ToList());

        private void OnEnterArea(Node2D body)
        {
            if (body is Player p && p.FirstSpawn)
            {
                CutScene?.Invoke("Awaking");
                p.FirstSpawn = false;
            }
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
    }
}
