namespace Playground
{
    using Godot;
    using Playground.Script.Helpers;

    public partial class MainScene : Node2D
    {
        private PackedScene? _battleScene = ResourceLoader.Load<PackedScene>("res://Scenes/BattleScene.tscn");
        private GlobalSignals? _globalSignals;
        private Player? _player;
        private EnemyAI? _enemy;

        public override void _Ready()
        {
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _globalSignals.PlayerEncounted += PlayerInteracted;
            _player = GetNode<Player>("CharacterBody2D");
            _enemy = GetNode<EnemyAI>("Enemy");
        }

        private void PlayerInteracted()
        {
            Node battleInstance = _battleScene!.Instantiate();
            Node mainScene = GetParent().GetNode<MainScene>("MainScene");
            mainScene.CallDeferred("add_child", battleInstance);
            _player!.PlayerLastPosition = _player.Position;

            BattleScene? battleScene = battleInstance as BattleScene;
            battleScene?.Init(_player!, _enemy!);
        }
    }
}

