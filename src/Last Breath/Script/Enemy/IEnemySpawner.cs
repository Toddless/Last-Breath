namespace Playground.Script.Enemy
{
    using System.Collections.Generic;
    using Godot;

    public interface IEnemySpawner
    {
        void DeleteEnemy(EnemyAI enemyToDelete);
        void InitializeEnemiesPositions(List<Vector2> positions);
        void SpawnNewEnemy();
    }
}