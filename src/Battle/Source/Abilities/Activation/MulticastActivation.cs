namespace Battle.Source.Abilities.Activation
{
    using Core.Battle;
    using Core.Entity;
    using Core.Enums;
    using Core.Services;
    using Godot;

    /// <summary>
    /// The intelligence-stance activation roll: every cast lands on a stage, higher stages are rarer.
    /// Top-down roll, chance = clamp(base × (1 + owner's MulticastChance), cap); stage 1 always fires.
    /// Stance-wide numbers come from the combat rules; per-ability/per-build shifts come from decorators.
    /// </summary>
    public class MulticastActivation
    {
        private const int BaseStage = 1;

        /// <summary>Per-ability multicast bonus on top of the owner's MulticastChance (upgrades set it).</summary>
        public float BonusChance { get; set; }

        public int Roll(IFightable owner) => Roll(owner, ResolveRules());

        /// <summary>The roll against explicit rules; the entry point above takes them from the combat
        /// rules catalog.</summary>
        public int Roll(IFightable owner, MulticastRules rules)
        {
            float multicast = owner.Parameters.GetValueForParameter(EntityParameter.MulticastChance) + BonusChance;
            foreach (MulticastStage stage in rules.Stages)
            {
                float chance = Mathf.Clamp(stage.BaseChance * (1 + multicast), 0f, stage.Cap);
                if (CombatRandom.Rolls.RandFloat() <= chance) return stage.Stage;
            }

            return BaseStage;
        }

        /// <summary>The stance figures the combat rules carry, taken from the composition at the moment
        /// of the cast; where no rules can be reached the stance rolls on the working defaults, because a
        /// host composing no services still casts and a roll without figures would never leave stage 1.</summary>
        private static MulticastRules ResolveRules() =>
            GameServiceProvider.TryGet<ICombatRulesProvider>()?.Multicast ?? MulticastRules.Default;
    }
}
