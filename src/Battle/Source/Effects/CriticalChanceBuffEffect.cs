namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Взор охотника": raises the bearer's critical chance by <c>value</c> (0.15 = +15%) per stack.</summary>
    public class CriticalChanceBuffEffect(
        int duration,
        int maxStacks,
        EffectValue value)
        : ParameterChangeEffect(
            id:"Effect_Critical_Chance_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.CriticalChance,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareGained)
    {
        public override IEffect Copy() => new CriticalChanceBuffEffect(Duration, MaxStacks, Authored);
    }
}
