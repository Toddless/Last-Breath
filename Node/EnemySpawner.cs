namespace Playground
{
    using Godot;

    public partial class EnemySpawner : Node
    {
        private MainScene? _parentScene;
        private PackedScene? _scene;
        private EnemyAI? _enemyAI;


        public EnemyAI? EnemyAI
        {
            get => _enemyAI;
            set => _enemyAI = value;
        }


        public override void _Ready()
        {
            _parentScene = GetParent().GetNode<MainScene>("/root/MainScene");
            _scene = ResourceLoader.Load<PackedScene>("res://Node/Enemy.tscn");
            GD.Print("Instantiate: EnemySpawner");
        }

        public void Initialize()
        {
            EnemyAI enemy = _scene!.Instantiate<EnemyAI>();
            _enemyAI = enemy;
            _parentScene!.CallDeferred("add_child", _enemyAI);
        }

        public async void CreateNewEnemy()
        {
            await ToSignal(_parentScene!, "EnemyCanSpawn");
            Initialize();
        }
    }
}
