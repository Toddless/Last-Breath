namespace Playground.Script
{
    public class BattleResult(Player player, BaseEnemy enemy, bool enemyKilled)
    {
        public Player Player { get; set; } = player;

        public BaseEnemy Enemy { get; set; } = enemy;

        public bool EnemyKilled { get; set; } = enemyKilled;
    }
}
