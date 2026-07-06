namespace Core.Ai
{
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// Runtime combat behavior of one NPC archetype (built from NpcBehavior.json).
    /// The archetype follows the stance: Dexterity = aggressive, Strength = defensive,
    /// Intelligence = mixed. All planner tuning lives here — the planner itself is stateless.
    /// </summary>
    public class BehaviorProfile
    {
        public required string Id { get; init; }
        public Stance Stance { get; init; }
        public AiIntellect Intellect { get; init; } = AiIntellect.Simple;

        /// <summary>Free casts allowed before the closing basic attack (turn model: abilities are free).</summary>
        public int MaxCastsPerTurn { get; init; } = 1;

        /// <summary>Scoring noise amplitude 0..1 — deliberate imperfection and the difficulty knob.</summary>
        public float Temperature { get; init; } = 0.15f;

        /// <summary>Multiplier for Damage/Debuff action scores.</summary>
        public float Aggression { get; init; } = 1f;

        /// <summary>Multiplier for Heal/Buff/Control action scores.</summary>
        public float Caution { get; init; } = 1f;

        /// <summary>Charged abilities: stage = MaxAffordableStage * Greed (clamped to at least 1).</summary>
        public float Greed { get; init; } = 1f;

        /// <summary>Casts scoring below this are not worth a slot — the NPC proceeds to the basic attack.</summary>
        public float CastScoreThreshold { get; init; } = 0.35f;

        /// <summary>Ability pool of the archetype keyed by ability id.</summary>
        public required IReadOnlyDictionary<string, AbilityBehavior> Abilities { get; init; }

        public AbilityBehavior? GetBehaviorFor(string abilityId) =>
            Abilities.TryGetValue(abilityId, out var behavior) ? behavior : null;
    }
}
