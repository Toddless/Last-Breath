namespace LastBreath.Script
{
    using Core.Enums;

    public class BattleResult(ICharacter player, ICharacter enemy, BattleResults result)
    {
        public ICharacter Player { get; set; } = player;

        public ICharacter Enemy { get; set; } = enemy;

        public BattleResults Results { get; set; } = result;
    }
}
