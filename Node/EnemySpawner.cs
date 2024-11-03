namespace Playground
{
    using Godot;

    public partial class EnemySpawner : Node
    {
        private RandomNumberGenerator _rnd = new();
        private MainScene? _parentScene;
        private PackedScene? _scene;

        public override void _Ready()
        {
            _parentScene = GetNode<MainScene>("/root/MainScene");
            _scene = ResourceLoader.Load<PackedScene>("res://Node/Enemy.tscn");
            GD.Print("Instantiate: EnemySpawner");
            _parentScene.EnemyCanSpawn += CreateNewEnemy;
        }

        public void Initialize()
        {
            EnemyAI enemy = _scene!.Instantiate<EnemyAI>();
            _parentScene!.CallDeferred("add_child", enemy);
            enemy.GetNode<Area2D>("Area2D").BodyEntered += enemy.PlayerEntered;
            enemy.Position = new Vector2(_rnd.RandfRange(50,900 ), _rnd.RandfRange(250, 700));
            _parentScene.EnemyAI = enemy;
        }

        public void CreateNewEnemy()
        {
            Initialize();
        }
    }
}
