namespace Battle.Source.Conditions
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class HealthThresholdExecuteCondition(float baseThreshold = 0.3f) : IExecuteCondition
    {
        public bool ShouldExecute(IEntity target, IAttackContext? context = null)
        {
            float hpPercent = target.CurrentHealth / target.Parameters.MaxHealth;
            float threshold = target is INpc npc ? baseThreshold - npc.EntityType.ConvertEntityTypeToThresholdPenalty() : baseThreshold;
            return hpPercent <= threshold;
        }
    }
}
