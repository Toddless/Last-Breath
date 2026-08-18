namespace Battle.Source.Abilities.ChainLightning
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Data.AbilityData;
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
    }

    /// <summary>
    /// Strikes the target and jumps to random enemies, losing a share of damage per jump.
    /// Stage 2 adds a jump, stage 3 softens the falloff, stage 4 doubles the last jump.
    /// The chain sequence is its own plan shape — exactly why the multicast base is generic.
    /// </summary>
    public class ChainLightning(AbilityBaseData data) : MulticastAbility<ChainPlan>(data)
    {
        public float Damage => this[AbilityParameter.Damage];
        public float WeaponDamageScale => this[AbilityParameter.WeaponDamageScale];
        public float SpellDamageScale => this[AbilityParameter.SpellDamageScale];

        public int Jumps => (int)this[Parameters.Jumps];
        public float DamageFalloff => this[Parameters.DamageFalloff];

        /// <summary>L3 upgrade point: the lightning ignores elemental resistances.</summary>
        public bool IgnoreResistances { get; set; }

        public static class Parameters
        {
            public const string Jumps = nameof(Jumps);
            public const string DamageFalloff = nameof(DamageFalloff);
            public const string StageThreeFalloffReduction = nameof(StageThreeFalloffReduction);
            public const string StageFourLastJumpMultiplier = nameof(StageFourLastJumpMultiplier);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterDamageParameters(parameters);
            parameters.RegisterDefault(Parameters.Jumps, 2);
            parameters.RegisterDefault(Parameters.DamageFalloff, 0.25f);
            parameters.RegisterDefault(Parameters.StageThreeFalloffReduction, 0.15f);
            parameters.RegisterDefault(Parameters.StageFourLastJumpMultiplier, 2f);
        }

        public override IAbility Copy() =>
            CopyUpgradesTo(new ChainLightning(Data) { IgnoreResistances = IgnoreResistances });

        protected override ChainPlan CreateBasePlan(List<IFightable> targets, IFightable owner, IBattleField field) =>
            new()
            {
                Damage = Damage,
                WeaponDamageScale = WeaponDamageScale,
                SpellDamageScale = SpellDamageScale,
                Jumps = Jumps,
                DamageFalloff = DamageFalloff,
                IgnoreResistances = IgnoreResistances,
                Targets = targets
            };

        protected override void ApplyStage(int stage, ChainPlan plan, IFightable owner, IBattleField field)
        {
            switch (stage)
            {
                case 2:
                    plan.Jumps += 1;
                    break;
                case 3:
                    plan.DamageFalloff = Mathf.Max(0f, plan.DamageFalloff - this[Parameters.StageThreeFalloffReduction]);
                    break;
                case 4:
                    plan.LastJumpMultiplier = this[Parameters.StageFourLastJumpMultiplier];
                    break;
            }
        }

        protected override async Task ExecutePlan(ChainPlan plan, IFightable owner, IBattleField field)
        {
            var initial = plan.Targets.FirstOrDefault(t => t.IsAlive);
            if (initial == null) return;

            float strikeDamage = plan.Damage + (owner.Parameters.Damage * plan.WeaponDamageScale) + (owner.Parameters.SpellDamage * plan.SpellDamageScale);
            IFightable? previous = null;
            int totalStrikes = 1 + plan.Jumps;
            for (int strike = 0; strike < totalStrikes; strike++)
            {
                var target = strike == 0 ? initial : PickJumpTarget(field, owner, previous);
                if (target == null) break;

                float hitDamage = strikeDamage * Mathf.Pow(1 - plan.DamageFalloff, strike);
                if (strike == totalStrikes - 1) hitDamage *= plan.LastJumpMultiplier;

                bool isCritical = RollCritical(owner);
                if (isCritical) hitDamage *= this.CriticalMultiplierOf(owner);

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
                // The first strike lands on the chosen target like any other direct hit; everything after
                // it is the chain hopping, and a jump is not a hit — its damage is the falloff's and its
                // target is the pick's, so a rider bought for hits must not read it as one.
                await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, isCritical, DamageSnapshot.From(context))
                {
                    Source = this,
                    Kind = strike == 0 ? ImpactKind.Hit : ImpactKind.ChainJump
                });
                previous = target;
            }
        }

        private IFightable? PickJumpTarget(IBattleField field, IFightable owner, IFightable? previous)
        {
            var alive = field.GetEnemies(owner).Where(e => e.IsAlive).ToList();
            var pool = alive.Count > 1 && previous != null ? alive.Where(e => !e.IsSame(previous.InstanceId)).ToList() : alive;
            return pool.Count == 0 ? null : pool[CombatRandom.Rolls.RandIntRange(0, pool.Count - 1)];
        }
    }
}
