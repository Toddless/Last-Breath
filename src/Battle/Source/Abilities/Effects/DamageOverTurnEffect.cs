namespace Battle.Source.Abilities.Effects
{
    using Godot;
    using Utilities;
    using Core.Data;
    using Core.Enums;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;

    public class DamageOverTurnEffect(
        int duration,
        int maxStacks = 999,
        StatusEffects statusEffect = StatusEffects.None,
        float percentFromDamage = 0.7f)
        : Effect(id: "Effect_Damage_Over_Turn", duration, maxStacks, statusEffect)
    {
        public float PercentFromBase { get; } = percentFromDamage;
        public float DamagePerTick { get; set; }

        public override async Task Apply(EffectApplyingContext context)
        {
            DamagePerTick = context.Damage * PercentFromBase;
            await base.Apply(context);
        }

        public override void TurnEnd()
        {
            if (Context.HasValue) Target?.Effects.RegisterDotTick(new DotTick(DamagePerTick, Status, Id, Context.Value.Caster));
            base.TurnEnd();
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not DamageOverTurnEffect other) return false;

            return other.DamagePerTick > DamagePerTick;
        }

        protected override string FormatDescription()
        {
            float damage = Target?.Effects.GetBy(x => x.Id == Id).Cast<DamageOverTurnEffect>().Sum(x => x.DamagePerTick) ?? DamagePerTick;
            return Localization.LocalizeDescriptionFormated(Id, Mathf.RoundToInt(damage));
        }

        public override IEffect Copy() => new DamageOverTurnEffect(Duration, MaxStacks, Status, PercentFromBase);
    }
}
