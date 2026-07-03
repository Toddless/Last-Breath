namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// Skip-turn debuff: applies <see cref="StatusEffects.Stun"/>, which the turn loop checks —
    /// the fighter's turn starts and immediately ends (effects, dots and cooldowns still tick,
    /// only the action phase is skipped; see BattleArena.RunBattleAsync / TurnSkippedEvent).
    /// The status is lifted automatically with the last carrier of it.
    /// </summary>
    public class StunEffect(int duration) : Effect(id: "Effect_Stun", duration, maxStacks: 1, StatusEffects.Stun)
    {
        public override IEffect Copy() => new StunEffect(Duration);
    }
}
