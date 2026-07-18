namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Иссушение разума": reduces the target's mana recovery by <c>value</c> (0.15 = −15%) per stack.</summary>
    public class MindDrainEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Mind_Drain",
            duration,
            maxStacks,
            value: 1 - value,
            parameter: EntityParameter.ManaRecovery,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        // Copy takes the primary-ctor value, not the transformed base Value — re-inverting would flip it.
        public override IEffect Copy() => new MindDrainEffect(Duration, MaxStacks, value);
    }
}
