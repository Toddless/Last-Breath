namespace Playground
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class MainScene : Node2D
    {
        private PackedScene? _battleScene = ResourceLoader.Load<PackedScene>("res://Scenes/BattleScene.tscn");
        private GlobalSignals? _globalSignals;
        private BattleScene? _currentBattleScene;
        private EnemySpawner? _enemySpawner;
        private Player? _player;
        private EnemyAI? _enemy;

        public EnemyAI? EnemyAI
        {
            get => _enemy;
            set => _enemy = value;
        }


        [Signal]
        public delegate void EnemyCanSpawnEventHandler();
        [Signal]
        public delegate void EnemyInitializedEventHandler();

        public override void _Ready()
        {
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _globalSignals.PlayerEncounted += PlayerInteracted;
            _enemySpawner = GetNode<EnemySpawner>("/root/MainScene/EnemySpawner");
            _enemySpawner.Initialize();
            _player = GetNode<Player>("CharacterBody2D");
            GD.Print("Instantiate: MainScene");
            EmitSignal(SignalName.EnemyInitialized);
        }

        private void PlayerInteracted()
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
            _currentBattleScene.Init(_player!, _enemy!);
            _currentBattleScene.BattleSceneFinished += OnBattleFinished;
            GD.Print("Battle ready");
        }

        private void OnBattleFinished()
        {
            _currentBattleScene = null;
            EmitSignal(SignalName.EnemyCanSpawn);
        }
    }
}

