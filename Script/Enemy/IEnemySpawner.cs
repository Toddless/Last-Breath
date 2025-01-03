namespace Playground.Script.Enemy
{
    using Godot;
    using Playground.Script.Scenes;

    public interface IEnemySpawner
    {
        void SpawnNewEnemy(Vector2 respawnPositions, PackedScene scene, BaseSpawnableScene parent);
    }
}
