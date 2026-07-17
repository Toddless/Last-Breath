namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// The anti-oneshot floor of a staged boss: granted by the stages controller while a transition
    /// is pending, stripped by the transformation together with everything else. Effectively
    /// permanent while it holds — the transition, not time, removes it.
    /// </summary>
    public class StageGuardEffect(float floorHealth) : Effect(id: "Effect_Stage_Guard", duration: 999, maxStacks: 1, StatusEffects.None), IStageGuardEffect
    {
        public float FloorHealth { get; } = floorHealth;

        public override IEffect Copy() => new StageGuardEffect(FloorHealth);
    }
}
