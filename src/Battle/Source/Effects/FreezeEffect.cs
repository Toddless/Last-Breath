namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// Skip-turn debuff, the cold twin of <see cref="StunEffect"/>: applies <see cref="StatusEffects.Freeze"/>,
    /// which the turn loop checks the same way (see BattleArena.RunBattleAsync / TurnSkippedEvent).
    /// </summary>
    public class FreezeEffect(int duration) : Effect(id: "Effect_Freeze", duration, maxStacks: 1, StatusEffects.Freeze)
    {
        public override IEffect Copy() => new FreezeEffect(Duration);
    }
}
