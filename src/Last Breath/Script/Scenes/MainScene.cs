namespace Playground
{
    using System.ComponentModel;
    using System.Linq;
    using Godot;
    using Playground.Script.Enemy;
    using Playground.Script.Scenes;
    using System;
    using System.Collections.Generic;
    using Playground.Script;
    using Playground.Script.Helpers;

    public partial class MainScene : BaseSpawnableScene
    {
        private PackedScene? _battleScene = ResourceLoader.Load<PackedScene>(ScenePath.BattleScene);
        private BattleScene? _currentBattleScene;
        private Queue<Action> _actionQueue = new();
        private Player? _player;
      
        public override void _Ready()
        {
            EnemySpawner = GetNode<IEnemySpawner>(nameof(EnemySpawner));
            _player = GetNode<Player>(nameof(CharacterBody2D));
            
            ResolveDependencies();
            InitializeEnemies();
        }

        private void InitializeEnemies()
        {
            EnemiesRespawnPosition = new List<Vector2>()
                {
                    new(259, 566),
                    new(728, 624),
                    new(878, 306),
                    new(515, 172),
                    new(290, 210),
                };
            EnemySpawner?.InitializeEnemiesPositions(EnemiesRespawnPosition);

            for (int i = 0; i < EnemiesRespawnPosition.Count; i++)
            {
                EnemySpawner?.SpawnNewEnemy();
            }
        }

        public void PlayerInteracted(BaseEnemy? enemy)
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

        private void OnBattleFinished(BaseEnemy enemyToDelete)
        {
            CallDeferred("remove_child", _currentBattleScene!);
            _currentBattleScene = null;
            enemyToDelete.PropertyChanged -= EnemyPropertyChanged;
            enemyToDelete.GetNode<Area2D>("Area2D").BodyEntered -= enemyToDelete.PlayerEntered;
            enemyToDelete.GetNode<Area2D>("Area2D").BodyExited -= enemyToDelete.PlayerExited;
            EnemySpawner!.DeleteEnemy(enemyToDelete);
            Enemies!.Remove(enemyToDelete);
        }

        public override void EnemyPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            PlayerInteracted(Enemies!.FirstOrDefault(x => x.PlayerEncounter == true));
        }

        protected override void ResolveDependencies() => DiContainer.InjectDependencies(this);
    }
}
