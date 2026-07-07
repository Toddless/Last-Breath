namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Проклятье увядания": reduces the target's max health by <c>value</c> (0.15 = −15%) per stack.</summary>
    public class WitheringCurseEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Withering_Curse",
            duration,
            maxStacks,
            value: 1 - value,
            parameter: EntityParameter.Health,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.Cursed)
    {
        // Copy takes the primary-ctor value, not the transformed base Value — re-inverting would flip it.
        public override IEffect Copy() => new WitheringCurseEffect(Duration, MaxStacks, value);
    }
}
