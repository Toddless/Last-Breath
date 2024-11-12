namespace Playground
{
    using Godot;

    public partial class EnemySpawner : Node
    {
        private RandomNumberGenerator _rnd = new();
        private MainScene? _parentScene;
        private Timer? _timer;
        private PackedScene? _scene;

        public override void _Ready()
        {
            _parentScene = (MainScene)GetParent();
            _scene = ResourceLoader.Load<PackedScene>("res://Node/Enemy.tscn");
            _timer = _parentScene.GetNode<Timer>($"{nameof(EnemySpawner)}/{nameof(Timer)}");
        }

        public void Initialize(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                SpawnNewEnemy();
            }
        }

        public void SpawnNewEnemy(int timer = default)
        {
            if(timer != 0)
            {
                _timer!.Start(timer);
            }
            EnemyAI enemy = _scene!.Instantiate<EnemyAI>();
            _parentScene!.CallDeferred("add_child", enemy);
            enemy.Position = new Vector2(_rnd.RandfRange(50, 900), _rnd.RandfRange(250, 700));
            enemy.GetNode<Area2D>("Area2D").BodyEntered += enemy.PlayerEntered;
            enemy.GetNode<Area2D>("Area2D").BodyExited += enemy.PlayerExited;
            enemy.PropertyChanged += _parentScene.EnemiePropertyChanged;
            _parentScene.Enemies!.Add(enemy);
        }
    }
}
