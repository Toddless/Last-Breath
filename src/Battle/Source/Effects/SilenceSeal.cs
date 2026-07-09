namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Forbids using active abilities while present: applies <see cref="StatusEffects.Paralysis"/>,
    /// which is checked by the activation gate (Ability.Execute / CanActivate).
    /// The status is lifted automatically with the last carrier of it.
    /// </summary>
    public class SilenceSeal(int duration = 3)
        : Effect(id: "Effect_Seal_Of_Silence", duration, maxStacks: 1, StatusEffects.Paralysis)
    {
        public override IEffect Copy() => new SilenceSeal(Duration);
    }
}
