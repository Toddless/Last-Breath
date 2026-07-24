namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>"Гниение": reduces the target's health recovery by <c>value</c> (0.15 = −15%) per stack.</summary>
    public class RotEffect(int duration, int maxStacks, float value)
        : ParameterChangeEffect(id: "Effect_Rot",
            duration,
            maxStacks,
            value: 1 - value,
            parameter: EntityParameter.HealthRecovery,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override bool IsHarmful => true;

        // Copy takes the primary-ctor value, not the transformed base Value — re-inverting would flip it.
        public override IEffect Copy() => new RotEffect(Duration, MaxStacks, value);
    }
}
