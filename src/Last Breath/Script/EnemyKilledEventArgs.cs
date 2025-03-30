namespace Playground.Script
{
    using Playground.Script.Enemy;
    using System;

    public class EnemyKilledEventArgs(string enemyId, EnemyType enemyType) : EventArgs
    {
        public string EnemyId { get; } = enemyId;
        public EnemyType EnemyType { get; } = enemyType;
    }
}
