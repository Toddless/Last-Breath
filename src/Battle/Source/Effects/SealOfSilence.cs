namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// Forbids using active abilities while present: applies <see cref="StatusEffects.Paralysis"/>,
    /// which is checked by the activation gate (Ability.Execute / CanActivate).
    /// The status is lifted automatically with the last carrier of it.
    /// </summary>
    public class SealOfSilence(int duration, int maxStacks = 1)
        : Effect(id: "Effect_Seal_Of_Silence", duration, maxStacks, StatusEffects.Paralysis)
    {
        public override IEffect Copy() => new SealOfSilence(Duration, MaxStacks);
    }
}
