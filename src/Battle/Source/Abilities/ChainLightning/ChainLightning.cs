namespace Battle.Source.Abilities.ChainLightning
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Components;
    using Core.Components.Decorator;
    using Core.Components.Module;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Godot;

    /// <summary>Cast plan of the Chain Lightning: the strike sequence knobs, mutated by stages.</summary>
    public class ChainPlan
    {
        public float Damage { get; set; }
        public float WeaponDamageScale { get; set; }
        public float SpellDamageScale { get; set; }
        public int Jumps { get; set; }
        public float DamageFalloff { get; set; }
        public float LastJumpMultiplier { get; set; } = 1f;
        public bool IgnoreResistances { get; set; }
        public List<IFightable> Targets { get; set; } = [];
        public IBattleField? Field { get; set; }
    }

    /// <summary>
    /// Strikes the target and jumps to random enemies, losing a share of damage per jump.
    /// Stage 2 adds a jump, stage 3 softens the falloff, stage 4 doubles the last jump.
    /// The chain sequence is its own plan shape — exactly why the multicast base is generic.
    /// </summary>
    public class ChainLightning(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        int jumps,
        float damageFalloff,
        float stageThreeFalloffReduction,
        float stageFourLastJumpMultiplier,
        Costs costType = Costs.Mana)
        : MulticastAbility<ChainPlan>(id: "Ability_Chain_Lightning", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
    {
        private readonly RandomNumberGenerator _rnd = CreateRandom();

        private float this[Parameters parameter] => AbilityParametersModuleManager.GetModule(parameter).GetValue();

        private IModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>> AbilityParametersModuleManager
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<Parameters, IParameterModule<Parameters>, AbilityParameterDecorator<Parameters>>(new()
                {
                    [Parameters.Jumps] = new Module<Parameters>(() => jumps, Parameters.Jumps),
                    [Parameters.DamageFalloff] = new Module<Parameters>(() => damageFalloff, Parameters.DamageFalloff)
                });
                return field;
            }
        }


        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                AddModuleValues(values, AbilityParametersModuleManager);
                return values;
            }
        }

        public int Jumps => (int)this[Parameters.Jumps];
        public float DamageFalloff => this[Parameters.DamageFalloff];

        /// <summary>L3 upgrade point: the lightning ignores elemental resistances.</summary>
        public bool IgnoreResistances { get; set; }

        public enum Parameters : byte
        {
            Jumps,
            DamageFalloff
        }

        public override void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
        {
            if (decorator is not AbilityParameterDecorator<Parameters> parameterDecorator)
            {
                base.AddParameterDecorator(decorator);
                return;
            }

            AbilityParametersModuleManager.AddDecorator(parameterDecorator);
        }

        public override void RemoveParameterDecorator<T>(string id, T key)
        {
            if (key is not Parameters parameter)
            {
                base.RemoveParameterDecorator(id, key);
                return;
            }

            AbilityParametersModuleManager.RemoveDecorator(id, parameter);
        }

        public override IAbility Copy()
        {
            var copy = new ChainLightning(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale,
                Jumps, DamageFalloff, stageThreeFalloffReduction, stageFourLastJumpMultiplier, CostType) { IgnoreResistances = IgnoreResistances };
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }

        protected override ChainPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new()
            {
                Damage = Damage,
                WeaponDamageScale = WeaponDamageScale,
                SpellDamageScale = SpellDamageScale,
                Jumps = Jumps,
                DamageFalloff = DamageFalloff,
                IgnoreResistances = IgnoreResistances,
                Targets = targets,
                Field = field
            };

        protected override void ApplyStage(int stage, ChainPlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.Jumps += 1;
                    break;
                case 3:
                    plan.DamageFalloff = Mathf.Max(0f, plan.DamageFalloff - stageThreeFalloffReduction);
                    break;
                case 4:
                    plan.LastJumpMultiplier = stageFourLastJumpMultiplier;
                    break;
            }
        }

        protected override async Task ExecutePlan(ChainPlan plan, IFightable owner)
        {
            var initial = plan.Targets.FirstOrDefault(t => t.IsAlive);
            if (initial == null || plan.Field == null) return;

            float strikeDamage = plan.Damage + (owner.Parameters.Damage * plan.WeaponDamageScale) + (owner.Parameters.SpellDamage * plan.SpellDamageScale);
            IFightable? previous = null;
            int totalStrikes = 1 + plan.Jumps;
            for (int strike = 0; strike < totalStrikes; strike++)
            {
                var target = strike == 0 ? initial : PickJumpTarget(plan.Field, owner, previous);
                if (target == null) break;

                float hitDamage = strikeDamage * Mathf.Pow(1 - plan.DamageFalloff, strike);
                if (strike == totalStrikes - 1) hitDamage *= plan.LastJumpMultiplier;

                bool isCritical = RollCritical(owner);
                if (isCritical) hitDamage *= owner.Parameters.CriticalDamage + CriticalDamageBonus;

                var context = new DamageContext
                {
                    Source = owner,
                    Cause = DamageCause.Ability,
                    IsCrit = isCritical,
                    CastId = CastId,
                    IgnoreResistances = plan.IgnoreResistances
                };
                context.Add(DamageType.Lightning, hitDamage);
                await target.TakeDamage(context);
                previous = target;
            }
        }

        private IFightable? PickJumpTarget(IBattleField field, IFightable owner, IFightable? previous)
        {
            var alive = field.GetEnemies(owner).Where(e => e.IsAlive).ToList();
            var pool = alive.Count > 1 && previous != null ? alive.Where(e => !e.IsSame(previous.InstanceId)).ToList() : alive;
            return pool.Count == 0 ? null : pool[_rnd.RandiRange(0, pool.Count - 1)];
        }

        private static RandomNumberGenerator CreateRandom()
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            return rnd;
        }
    }
}
