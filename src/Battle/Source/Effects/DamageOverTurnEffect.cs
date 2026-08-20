namespace Battle.Source.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Enums;
    using Core.Services;
    using Godot;

    public class DamageOverTurnEffect(
        int duration,
        StatusEffects statusEffect,
        int maxStacks,
        EffectValue percentFromDamage)
        : Effect(IdFor(statusEffect), duration, maxStacks, statusEffect), IDamageOverTurnEffect
    {
        public override bool IsHarmful => true;

        /// <summary>The ceiling a source names when it holds no opinion about one: the base class takes
        /// the stack count down to whatever the canon balances the status at, so a figure above every
        /// canonical ceiling means "as many as the canon allows".</summary>
        public const int NoCeilingOfItsOwn = 999;

        private const string BaseId = "Effect_Damage_Over_Turn";

        /// <summary>What a tick carries where no canon can be read at all — a working figure of a host
        /// that composed no registry (a bench, a tool), never a shipped one. Every shipped road either
        /// names its own share or takes the canonical one through <see cref="FromCanon"/>.</summary>
        private const float SandboxPercentFromDamage = 0.7f;

        /// <summary>The one figure a caster owns of a canonical stack: how long it lasts. Named as the
        /// registry declares it, because that is the key the canonical row is overridden under.</summary>
        private const string DurationKey = "duration";

        /// <summary>What each damaging status feeds on: the component of the blow its pool is taken from
        /// and the caster stat that multiplies that pool. Burning takes the fire of the blow and bleeding
        /// its physical part; POISON NAMES NO COMPONENT BY DESIGN — its pool is the whole blow, every
        /// component the hit landed, which is what the null stands for and not a kind left unwritten.
        /// A typeless DoT is not listed — it feeds on the whole blow and no stat raises it.</summary>
        private static readonly Dictionary<StatusEffects, (DamageType? Component, EntityParameter Multiplier)> s_pools = new()
        {
            [StatusEffects.Poison] = (null, EntityParameter.PoisonDamageMultiplier),
            [StatusEffects.Burning] = (DamageType.Fire, EntityParameter.BurningDamageMultiplier),
            [StatusEffects.Bleed] = (DamageType.Physical, EntityParameter.BleedDamageMultiplier)
        };

        // Fine for a getter: Description is read on EffectsChanged (tooltip rebuild), not per frame.
        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                var effects = Target?.Effects.GetBy(effect => effect.Id == Id).Cast<DamageOverTurnEffect>().ToList();
                values["Damage"] = Mathf.RoundToInt(effects?.Sum(x => x.DamagePerTick) ?? DamagePerTick);
                if (effects is { Count: > 0 }) values["Duration"] = effects.Max(x => x.Duration); // max across stacks
                return values;
            }
        }

        /// <summary>Share of the blow one tick carries, as authored — <see cref="Copy"/> hands it on
        /// unscaled. Whoever lays a stack names it: there is no figure to fall back on, because a share
        /// nobody wrote is balance nobody sees.</summary>
        public EffectValue PercentFromBase { get; } = percentFromDamage;

        /// <summary>Settable so effect-application mutators ("+X% burning damage") can scale the tick.</summary>
        public float DamagePerTick { get; set; }

        /// <summary>
        /// A stack of the status as the canon balances it: the caster hands over how long it lasts and
        /// nothing else, so what one tick carries and how many stacks may stand come from
        /// <c>SharedData/Effects</c> — moving the figure there moves every cast that lays this stack.
        /// <para>The registry is pulled from the composition at the moment of the question, like the
        /// stack ceiling of the base class and for the same reasons: nothing holds a provider that may
        /// have been discarded. A composed registry that refuses the id has already named the reason in
        /// the Tracker and nothing is laid; where no composition exists at all there is no canon to read
        /// and the stack is built at <see cref="SandboxPercentFromDamage"/> — a host's working answer,
        /// named out loud here and reachable from nowhere else.</para>
        /// </summary>
        public static IEffect? FromCanon(int duration, StatusEffects status)
        {
            IEffectProvider? canon = GameServiceProvider.TryGet<IEffectProvider>();
            if (canon == null) return new DamageOverTurnEffect(duration, status, NoCeilingOfItsOwn, SandboxPercentFromDamage);

            string id = IdFor(status);
            var owned = new Dictionary<string, float>(StringComparer.Ordinal) { [DurationKey] = duration };
            return canon.CreateEffect(id, new RecordProperties(id, owned));
        }

        public override async Task Apply(EffectApplyingContext context)
        {
            // Copies (transfer/bounce/spread) arrive with DamagePerTick already carried over via Copy().
            // Recalculating it here would scale the damage by PercentFromBase a second time.
            // Stamped ahead of base.Apply: the tick is derived here and the mutator pipeline below reads it.
            Effectiveness = context.Effectiveness;

            if (DamagePerTick == 0) DamagePerTick = PoolOf(context) * Effective(PercentFromBase);
            // A stack with nothing to tick with is not laid at all: it would hold a place under the
            // ceiling and evict a living stack of its own kind for the sake of ticking nothing.
            if (DamagePerTick <= 0) return;

            await base.Apply(context);
        }

        public override void TurnEnd()
        {
            // InstanceId, not Id: removing one stack must not cancel pending ticks of the other stacks
            if (Context.HasValue) Target?.Effects.RegisterDotTick(new DotTick(DamagePerTick, Status, InstanceId, Context.Value.Caster));
            base.TurnEnd();
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not DamageOverTurnEffect other) return false;

            return DamagePerTick > other.DamagePerTick;
        }

        public override IEffect Copy() => new DamageOverTurnEffect(Duration, Status, MaxStacks, PercentFromBase) { DamagePerTick = DamagePerTick };

        /// <summary>The blow this status feeds on, raised by the caster's stat for that kind. Both are read
        /// once, as the stack is laid: a caster who grows stronger afterwards moves his next stack, not this
        /// one. A source whose figure stands for the entire hit says so and the kind's component is skipped.</summary>
        private float PoolOf(EffectApplyingContext context)
        {
            if (!s_pools.TryGetValue(Status, out (DamageType? Component, EntityParameter Multiplier) pool)) return context.Damage.Total;

            float blow = pool.Component == null || context.PoolFromWholeHit
                ? context.Damage.Total
                : context.Damage[pool.Component.Value];
            return blow * (1 + context.Caster.Parameters.GetValueForParameter(pool.Multiplier));
        }

        /// <summary>One identity per damage kind: bleed, poison and burning are separate lines on the target.
        /// Identity is what the whole chain groups by — the HUD slot (icon, stack counter, duration), the
        /// summed damage in the description, and the MaxStacks bucket with its eviction — so sharing one Id
        /// merged the three into a single lying slot and let a fresh burn evict a poison stack. A typeless
        /// DoT keeps the shared identity. Names match the .po keys and the icon file names.</summary>
        private static string IdFor(StatusEffects status) =>
            status == StatusEffects.None ? BaseId : $"{BaseId}_{status}";
    }
}
