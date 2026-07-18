namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Всплеск силы": increases the target's spell damage by <c>value</c> (0.25 = +25%) per stack.</summary>
    public class SpellSurgeEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Spell_Surge",
            duration,
            maxStacks,
            value: 1 + value,
            parameter: EntityParameter.SpellDamage,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        // Copy takes the primary-ctor value, not the transformed base Value — re-transforming would double it.
        public override IEffect Copy() => new SpellSurgeEffect(Duration, MaxStacks, value);
    }
}
