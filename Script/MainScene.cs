namespace Playground
{
    using System.Collections.ObjectModel;
    using Playground.Script.Helpers;
    using Playground.Script;
    using System.ComponentModel;
    using System.Linq;
    using Godot.Collections;
    using Playground.Script.Enemy;
    using Godot;

    public partial class MainScene : ObservableNode2D
    {
        private PackedScene? _battleScene = ResourceLoader.Load<PackedScene>("res://Scenes/BattleScene.tscn");
        private RandomNumberGenerator _rnd = new();
        private ObservableCollection<EnemyAI>? _enemies = [];
        private BattleScene? _currentBattleScene;
        private Button? _reduceAttribute;
        private Button? _addAttribute;
        private Button? _dealDamage;
        private EnemyInventory? _enemyInventory;
        private GlobalSignals? _globalSignals;
        private EnemySpawner? _enemySpawner;
        private Player? _player;
        private int _level;

        public ObservableCollection<EnemyAI>? Enemies
        {
            get => _enemies;
            set => _enemies = value;
        }

        public Array<Vector2> EnemiesRespawnPosition
        {
            get;
        } =
            [
            new Vector2(259, 566),
            //new Vector2(728, 624),
            //new Vector2(878, 306),
            //new Vector2(515, 172),
            //new Vector2(290, 210),
            ];

        public override void _Ready()
        {
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _enemySpawner = GetNode<EnemySpawner>(nameof(EnemySpawner));
            _enemyInventory = GetNode<EnemyInventory>(NodePathHelper.EnemyInventory);
            _player = GetNode<Player>(nameof(CharacterBody2D));
            _reduceAttribute = GetNode<Button>("/root/MainScene/UI/Buttons/ReduceEnemiesAttribute");
            _addAttribute = GetNode<Button>("/root/MainScene/UI/Buttons/AddEnemiesAttribute");
            _dealDamage = GetNode<Button>("/root/MainScene/UI/Buttons/DealDamage");
            _dealDamage.Pressed += DealDamage;
            _addAttribute.Pressed += AddEnemiesAttribute;
            _reduceAttribute.Pressed += ReduceEnemiesAttributePressed;
            for (int i = 0; i < EnemiesRespawnPosition.Count; i++)
            {
                _enemySpawner.SpawnNewEnemy();
            }
        }

        private void DealDamage()
        {
            var enemy = _enemies[0];
            enemy.EnemyHealth.MaxHealth  *= 0.9f;
            GD.Print($"Current health: {enemy.EnemyHealth.CurrentHealth}\n" +
                $"Max health: {enemy.EnemyHealth.MaxHealth}");
        }

        private void AddEnemiesAttribute()
        {
            _enemies[0].EnemyAttribute.Dexterity.Total += 5;
            _enemies[0].EnemyAttribute.Strength.Total += 5;
        }

        private void ReduceEnemiesAttributePressed()
        {
            _enemies[0].EnemyAttribute.Dexterity.Total -= 5;
            _enemies[0].EnemyAttribute.Strength.Total -= 5;
        }

        public void PlayerInteracted(EnemyAI? enemy)
        {
            if (_currentBattleScene != null || enemy == null)
            {
                return;
            }

            Node battleInstance = _battleScene!.Instantiate();
            Node mainScene = GetParent().GetNode<MainScene>("MainScene");
            _currentBattleScene = (BattleScene)battleInstance;
            mainScene.CallDeferred("add_child", battleInstance);
            _player!.PlayerLastPosition = _player.Position;
            _currentBattleScene.Init(_player!, enemy);
            this.CallDeferred("remove_child", _player);
            _currentBattleScene.CallDeferred("add_child", _player);
            this.CallDeferred("remove_child", enemy);
            _currentBattleScene.CallDeferred("add_child", enemy);
            _currentBattleScene.BattleSceneFinished += OnBattleFinished;
        }

        private void OnBattleFinished(EnemyAI enemyToDelete)
        {
            CallDeferred("remove_child", _currentBattleScene!);
            _currentBattleScene = null;
            enemyToDelete.PropertyChanged -= EnemiePropertyChanged;
            enemyToDelete.GetNode<Area2D>("Area2D").BodyEntered -= enemyToDelete.PlayerEntered;
            enemyToDelete.GetNode<Area2D>("Area2D").BodyExited -= enemyToDelete.PlayerExited;
            _enemySpawner!.DeleteEnemy(enemyToDelete);
            _enemies!.Remove(enemyToDelete);
        }

        public void EnemiePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PlayerInteracted(_enemies!.FirstOrDefault(x => x.PlayerEncounted == true));
        }
    }
}
