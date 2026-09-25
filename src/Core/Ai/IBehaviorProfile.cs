namespace Core.Ai
{
    using System.Collections.Generic;
    using Enums;

    public interface IBehaviorProfile
    {
        string Id { get; init; }
        Stance Stance { get; init; }
        AiIntellect Intellect { get; init; }

        /// <summary>Free casts allowed before the closing basic attack (turn model: abilities are free).</summary>
        int MaxCastsPerTurn { get; init; }

        /// <summary>Scoring noise amplitude 0..1 — deliberate imperfection and the difficulty knob.</summary>
        float Temperature { get; init; }

        /// <summary>Multiplier for Damage/Debuff action scores.</summary>
        float Aggression { get; init; }

        /// <summary>Multiplier for Heal/Buff/Control action scores.</summary>
        float Caution { get; init; }

        /// <summary>Charged abilities: stage = MaxAffordableStage * Greed (clamped to at least 1).</summary>
        float Greed { get; init; }

        /// <summary>Casts scoring below this are not worth a slot — the NPC proceeds to the basic attack.</summary>
        float CastScoreThreshold { get; init; }

        /// <summary>Health ratio at which the NPC flees the battle instead of playing its turn. 0 = fearless.</summary>
        float FleeHealthThreshold { get; init; }

        /// <summary>Ability pool of the archetype keyed by ability id.</summary>
        IReadOnlyDictionary<string, AbilityBehavior> Abilities { get; init; }

        AbilityBehavior? GetBehaviorFor(string abilityId);
    }
}
