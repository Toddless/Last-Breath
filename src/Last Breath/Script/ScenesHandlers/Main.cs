namespace Playground.Script.ScenesHandlers
{
    using Godot;

    public partial class Main : Node2D
    {
        private MainWorld? _mainWorld;
        private CanvasLayer? _mainUI;
        private CanvasLayer? _battleLayer;
        private Player? _player;

        public override void _Ready()
        {
            _mainWorld = GetNode<MainWorld>("MainWorld");
            _mainUI = GetNode<CanvasLayer>("MainUILayer");
            _battleLayer = GetNode<CanvasLayer>("BattleLayer");
            _player = _mainWorld.GetNode<Player>(nameof(Player));
        }


        public void PlayerInteracted(BaseEnemy? enemy)
        {
            //Node battleInstance = _battleScene!.Instantiate();
            //Node mainScene = GetParent().GetNode<MainWorld>(nameof(MainWorld));
            //_currentBattleScene = (BattleUI)battleInstance;
            //mainScene.CallDeferred("add_child", battleInstance);
            //  _player!.PlayerLastPosition = _player.Position;
            //  _currentBattleScene.Init(_player!, enemy);
            // this.CallDeferred("remove_child", _player);
            //  _currentBattleScene.CallDeferred("add_child", _player);
            //this.CallDeferred("remove_child", enemy);
            //_currentBattleScene.CallDeferred("add_child", enemy);
            //  _currentBattleScene.BattleSceneFinished += OnBattleFinished;
        }

        private void OnBattleFinished(BaseEnemy enemyToDelete)
        {
            //CallDeferred("remove_child", _currentBattleScene!);
            //_currentBattleScene = null;
            //enemyToDelete.PropertyChanged -= EnemyPropertyChanged;
            //enemyToDelete.GetNode<Area2D>("Area2D").BodyEntered -= enemyToDelete.PlayerEntered;
            //enemyToDelete.GetNode<Area2D>("Area2D").BodyExited -= enemyToDelete.PlayerExited;
            //EnemySpawner!.DeleteEnemy(enemyToDelete);
            //Enemies!.Remove(enemyToDelete);
        }

    }
}
