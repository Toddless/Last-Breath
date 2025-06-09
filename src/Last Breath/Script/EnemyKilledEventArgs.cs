namespace Playground.Script
{
    using System;
    using Playground.Script.Enemy;

    public class EnemyKilledEventArgs(string enemyId, EnemyType enemyType) : EventArgs
    {
        public string EnemyId { get; } = enemyId;
        public EnemyType EnemyType { get; } = enemyType;
    }
}
