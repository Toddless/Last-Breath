namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Слабая регенерация": increases the target's health recovery by <c>value</c> (0.15 = +15%) per stack.</summary>
    public class WeakRegenerationEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Weak_Regeneration",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.HealthRecovery,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.Regeneration,
            shape: EffectValueShape.ShareGained)
    {
        // Copy takes the primary-ctor value, not the transformed base Value — re-transforming would double it.
        public override IEffect Copy() => new WeakRegenerationEffect(Duration, MaxStacks, value);
    }
}
