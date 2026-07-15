namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Entity;
    using Core.Entity.Components.Module;
    using Core.Enums;
    using Core.Events;
    using Godot;

    /// <summary>
    /// Base of the Intelligence stance abilities: every cast rolls an activation stage.
    /// Stages are cumulative — a stage-4 roll applies the mutations of stages 2 and 3 to the cast plan.
    /// <typeparamref name="TPlan"/> is the per-cast state shape: a damage volley, a shield, a debuff set —
    /// the base only guarantees the roll, the cumulative stage loop and that a plan lives exactly one cast.
    /// </summary>
    public abstract class MulticastAbility<TPlan>(
        string id,
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        Costs costType = Costs.Mana)
        : DamagingAbility(id, tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
        where TPlan : class
    {
        private const int BaseStage = 1;

        /// <summary>Stance-wide base chances per stage; per-ability/per-build shifts come from decorators, not data.</summary>
        private static readonly Dictionary<int, float> s_baseStageChances = new() { [2] = 0.5f, [3] = 0.25f, [4] = 0.05f };

        /// <summary>Stance-wide caps for the final stage chance: stage 2 may become guaranteed, higher stages may not.</summary>
        private static readonly Dictionary<int, float> s_stageChanceCaps = new() { [2] = 1f, [3] = 0.65f, [4] = 0.4f };

        private readonly RandomNumberGenerator _rnd = new();

        /// <summary>
        /// Bonus on top of entity's final critical chance. e.g 45% critical chance * 1.35 (35% bonus critical chance)
        /// </summary>
        public float CriticalChanceBonus => this[AbilityParameter.CriticalChanceBonus];
        public float CriticalDamageBonus => this[AbilityParameter.CriticalDamageBonus];

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            int stage = RollActivationStage(owner);
            owner.CombatEvents.Publish(new AbilityStageActivatedEvent(this, stage));

            var plan = CreateBasePlan(targets, owner, field);
            for (int current = BaseStage + 1; current <= stage; current++)
                ApplyStage(current, plan, owner, field);

            await ExecutePlan(plan, owner);
        }

        /// <summary>Top-down roll: chance = clamp(base * (1 + MulticastChance), cap). Stage 1 always fires.</summary>
        protected int RollActivationStage(IFightable owner)
        {
            float multicast = owner.Parameters.GetValueForParameter(EntityParameter.MulticastChance);
            foreach (int stage in s_baseStageChances.Keys.OrderByDescending(s => s))
            {
                float cap = s_stageChanceCaps.GetValueOrDefault(stage, 1f);
                float chance = Mathf.Clamp(s_baseStageChances[stage] * (1 + multicast), 0f, cap);
                if (_rnd.Randf() <= chance) return stage;
            }

            return BaseStage;
        }

        /// <summary>The stage-1 cast built from the ability's current (post-upgrade) parameters.</summary>
        protected abstract TPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field);

        /// <summary>Mutation of a single stage; called for every stage from 2 up to the rolled one.</summary>
        protected abstract void ApplyStage(int stage, TPlan plan, IFightable owner, IBattleField field);

        /// <summary>Executes the fully mutated plan.</summary>
        protected abstract Task ExecutePlan(TPlan plan, IFightable owner);

        protected override Dictionary<AbilityParameter, IParameterModule<AbilityParameter>> CreateBaseModules()
        {
            var modules = base.CreateBaseModules();
            modules[AbilityParameter.CriticalChanceBonus] = new Module<AbilityParameter>(() => 0f, AbilityParameter.CriticalChanceBonus);
            modules[AbilityParameter.CriticalDamageBonus] = new Module<AbilityParameter>(() => 0f, AbilityParameter.CriticalDamageBonus);
            return modules;
        }

        /// <summary>Ability bonus is a fractional increase over the owner's crit chance (0.35 = +35%).</summary>
        protected bool RollCritical(IFightable owner) =>
            _rnd.Randf() <= owner.Parameters.CriticalChance * (1 + CriticalChanceBonus);
    }
}
