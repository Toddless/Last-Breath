namespace Battle.Source.Abilities.Activation
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Entity;
    using Core.Enums;
    using Godot;

    /// <summary>
    /// The intelligence-stance activation roll: every cast lands on a stage, higher stages are rarer.
    /// Top-down roll, chance = clamp(base × (1 + owner's MulticastChance), cap); stage 1 always fires.
    /// Stance-wide numbers live here; per-ability/per-build shifts come from decorators, not data.
    /// </summary>
    public class MulticastActivation
    {
        private const int BaseStage = 1;

        /// <summary>Stance-wide base chances per stage.</summary>
        private readonly Dictionary<int, float> _baseStageChances = new() { [2] = 0.5f, [3] = 0.25f, [4] = 0.05f };

        /// <summary>Stance-wide caps for the final stage chance: stage 2 may become guaranteed, higher stages may not.</summary>
        private readonly Dictionary<int, float> _stageChanceCaps = new() { [2] = 1f, [3] = 0.65f, [4] = 0.4f };

        /// <summary>Per-ability multicast bonus on top of the owner's MulticastChance (upgrades set it).</summary>
        public float BonusChance { get; set; }

        public int Roll(IFightable owner)
        {
            float multicast = owner.Parameters.GetValueForParameter(EntityParameter.MulticastChance) + BonusChance;
            foreach (int stage in _baseStageChances.Keys.OrderByDescending(s => s))
            {
                float cap = _stageChanceCaps.GetValueOrDefault(stage, 1f);
                float chance = Mathf.Clamp(_baseStageChances[stage] * (1 + multicast), 0f, cap);
                if (CombatRandom.Rolls.RandFloat() <= chance) return stage;
            }

            return BaseStage;
        }
    }
}
