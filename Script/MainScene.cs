namespace Playground
{
    using System.Collections.ObjectModel;
    using Playground.Script.Helpers;
    using Playground.Script;
    using Godot;
    using System.ComponentModel;

    public partial class MainScene : ObservableObject
    {
        private PackedScene? _battleScene = ResourceLoader.Load<PackedScene>("res://Scenes/BattleScene.tscn");
        private BattleScene? _currentBattleScene;
        private EnemyInventory? _enemyInventory;
        private GlobalSignals? _globalSignals;
        private EnemySpawner? _enemySpawner;
        private ObservableCollection<EnemyAI>? _enemies = [];
        private int _maxEnemiesAtScene = 5;
        private Player? _player;

        public ObservableCollection<EnemyAI>? Enemies
        {
            get => _enemies;
            set
            {
                if(SetProperty(ref _enemies, value))
                {
                    GD.Print("Collection initialized");
                }
            }
        }


        [Signal]
        public delegate void MainSceneInitializedEventHandler();

        public override void _Ready()
        {
            Enemies = [];
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _enemySpawner = GetNode<EnemySpawner>("EnemySpawner");
            _enemySpawner.Initialize(_maxEnemiesAtScene);
            _enemyInventory = GetNode<EnemyInventory>(NodePathHelper.EnemyInventory);
            _player = GetNode<Player>("CharacterBody2D");
            GD.Print("Instantiate: MainScene");
            EmitSignal(SignalName.MainSceneInitialized);
        }

        public void PlayerInteracted(EnemyAI enemy)
        {
            if (_currentBattleScene != null)
            {
                return;
            }

            Node battleInstance = _battleScene!.Instantiate();
            Node mainScene = GetParent().GetNode<MainScene>("MainScene");
            _currentBattleScene = (BattleScene)battleInstance;
            mainScene.CallDeferred("add_child", battleInstance);
            _player!.PlayerLastPosition = _player.Position;
            _currentBattleScene.Init(_player!, enemy);
            _currentBattleScene.BattleSceneFinished += OnBattleFinished;
            GD.Print("Battle ready");
        }

        private void OnBattleFinished(EnemyAI enemyToDelete)
        {
            _currentBattleScene = null;
            enemyToDelete.PropertyChanged -= EnemiePropertyChanged;
            enemyToDelete.GetNode<Area2D>("Area2D").BodyEntered -= enemyToDelete.PlayerEntered;
            enemyToDelete.GetNode<Area2D>("Area2D").BodyExited -= enemyToDelete.PlayerExited;
            _enemies?.Remove(enemyToDelete);
            _enemySpawner!.SpawnNewEnemy();
        }

        public void EnemiePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            foreach (var item in _enemies!)
            {
                if (item.PlayerEncounted)
                {
                    PlayerInteracted(item);
                }
            }
        }
    }
}

