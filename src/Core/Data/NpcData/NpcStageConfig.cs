namespace Core.Data.NpcData
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// One parsed boss stage: base parameters are the definition originals × <see cref="ParameterMultiplier"/>,
    /// <see cref="Abilities"/> fully replace the ability book's content, <see cref="AttackEffects"/> ride
    /// every landed attack. <see cref="NextStageAtHealthPercent"/> arms the one-way transition to the next
    /// stage; <see cref="RageAtHealthPercent"/>/<see cref="RageBonus"/> arm the one-time rage burst.
    /// </summary>
    public record NpcStageConfig(
        float ParameterMultiplier,
        IReadOnlyList<string> Abilities,
        IReadOnlyList<NpcStageAttackEffectConfig> AttackEffects,
        float? NextStageAtHealthPercent,
        float? RageAtHealthPercent,
        float RageBonus);

    /// <summary>One on-attack effect of a stage: roll <see cref="Chance"/> per landed attack and apply
    /// the effect. <see cref="DamagePercent"/> is the effect's magnitude (share of the attack's damage
    /// for DoTs, share of max health for the curse).</summary>
    public record NpcStageAttackEffectConfig(
        StageAttackEffectKind Effect,
        float Chance,
        float DamagePercent,
        int Duration,
        int MaxStacks);
}
