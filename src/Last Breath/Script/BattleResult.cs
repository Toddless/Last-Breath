namespace LastBreath.Script
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class BattleResult(IEntity player, IEntity enemy, BattleResults result)
    {
        public IEntity Player { get; set; } = player;

        public IEntity Enemy { get; set; } = enemy;

        public BattleResults Results { get; set; } = result;
    }
}
