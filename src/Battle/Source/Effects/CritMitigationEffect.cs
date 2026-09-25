namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Buff: cuts the critical damage the bearer takes by <c>value</c> (0..1), additively.
    /// Carries its id so one mechanism can ship under several names, each with its own wording and canon.</summary>
    public class CritMitigationEffect(int duration, int maxStacks, EffectValue value, string id = "Effect_Crit_Mitigation")
        : ParameterChangeEffect(
            id,
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.CriticalDamageMitigation,
            type: OperationType.Add,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override IEffect Copy() => new CritMitigationEffect(Duration, MaxStacks, Authored, Id);
    }
}
