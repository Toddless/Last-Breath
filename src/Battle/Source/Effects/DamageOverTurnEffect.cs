namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Enums;
    using Godot;

    public class DamageOverTurnEffect(
        int duration,
        StatusEffects statusEffect = StatusEffects.None,
        int maxStacks = 999,
        float percentFromDamage = 0.7f)
        : Effect(IdFor(statusEffect), duration, maxStacks, statusEffect), IDamageOverTurnEffect
    {
        public override bool IsHarmful => true;

        private const string BaseId = "Effect_Damage_Over_Turn";

        public float PercentFromBase { get; } = percentFromDamage;

        /// <summary>Settable so effect-application mutators ("+X% burning damage") can scale the tick.</summary>
        public float DamagePerTick { get; set; }

        public override async Task Apply(EffectApplyingContext context)
        {
            // Copies (transfer/bounce/spread) arrive with DamagePerTick already carried over via Copy().
            // Recalculating it here would scale the damage by PercentFromBase a second time.

            if (DamagePerTick == 0) DamagePerTick = context.Damage * PercentFromBase;
            await base.Apply(context);
        }

        // TODO:
        // Развести типы урона: Горение наносит урон от огня, кровотечение и яд физический
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

        // Fine for a getter: Description is read on EffectsChanged (tooltip rebuild), not per frame.
        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                var effects = Target?.Effects.GetBy(x => x.Id == Id).Cast<DamageOverTurnEffect>().ToList();
                values["Damage"] = Mathf.RoundToInt(effects?.Sum(x => x.DamagePerTick) ?? DamagePerTick);
                if (effects is { Count: > 0 }) values["Duration"] = effects.Max(x => x.Duration); // max across stacks
                return values;
            }
        }

        public override IEffect Copy() => new DamageOverTurnEffect(Duration, Status, MaxStacks, PercentFromBase) { DamagePerTick = DamagePerTick };

        /// <summary>One identity per damage kind: bleed, poison and burning are separate lines on the target.
        /// Identity is what the whole chain groups by — the HUD slot (icon, stack counter, duration), the
        /// summed damage in the description, and the MaxStacks bucket with its eviction — so sharing one Id
        /// merged the three into a single lying slot and let a fresh burn evict a poison stack. A typeless
        /// DoT keeps the shared identity. Names match the .po keys and the icon file names.</summary>
        private static string IdFor(StatusEffects status) =>
            status == StatusEffects.None ? BaseId : $"{BaseId}_{status}";
    }
}
