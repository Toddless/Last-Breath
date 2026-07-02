namespace Battle.Source.Effects
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Godot;
    using Utilities;

    public class DamageOverTurnEffect(
        int duration,
        StatusEffects statusEffect = StatusEffects.None,
        int maxStacks = 999,
        float percentFromDamage = 0.7f)
        : Effect(id: "Effect_Damage_Over_Turn", duration, maxStacks, statusEffect)
    {
        public float PercentFromBase { get; } = percentFromDamage;
        public float DamagePerTick { get; private set; }

        public override async Task Apply(EffectApplyingContext context)
        {
            // Copies (transfer/bounce/spread) arrive with DamagePerTick already carried over via Copy().
            // Recalculating it here would scale the damage by PercentFromBase a second time.
            
            if (DamagePerTick == 0) DamagePerTick = context.Damage * PercentFromBase;
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

        protected override string FormatDescription()
        {
            float damage = Target?.Effects.GetBy(x => x.Id == Id).Cast<DamageOverTurnEffect>().Sum(x => x.DamagePerTick) ?? DamagePerTick;
            return Localization.LocalizeDescriptionFormated(Id, Mathf.RoundToInt(damage));
        }

        public override IEffect Copy() => new DamageOverTurnEffect(Duration, Status, MaxStacks, PercentFromBase) { DamagePerTick = DamagePerTick };
    }
}
