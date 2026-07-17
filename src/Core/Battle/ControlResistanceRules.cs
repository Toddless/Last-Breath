namespace Core.Battle
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>Parsed control-resistance rules (see CombatRules.json); enums are resolved at load time.</summary>
    public record ControlResistanceRules(
        StatusEffects HardControlMask,
        IReadOnlyList<float> DurationMultipliers,
        IReadOnlySet<EntityType> AppliesTo,
        int ResistanceDecayTurns)
    {
        public static readonly ControlResistanceRules Disabled = new(StatusEffects.None, [], new HashSet<EntityType>(), 1);
    }
}
