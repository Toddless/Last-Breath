namespace Playground
{
    using System.Collections.Generic;
    using Godot;

    public partial class EnemySpawner : Node
    {
        private PackedScene? _scene;
        private List<EnemyAI>? _enemies = [];
        private Node? _parentScene;

        public override void _PhysicsProcess(double delta)
        {

        }

        public override void _Ready()
        {
            _scene = ResourceLoader.Load<PackedScene>("res://Node/Enemy.tscn");
            _parentScene = GetParent().GetNode<MainScene>("/root/MainScene");
            CreateEnemies();
        }

        private void CreateEnemies()
        {
            for (int i = 0; i < 10; i++)
            {
                EnemyAI enemy = _scene!.Instantiate<EnemyAI>();
                _enemies!.Add(enemy);
            }
        }
    }
}
