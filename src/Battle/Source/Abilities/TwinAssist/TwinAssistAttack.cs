namespace Battle.Source.Abilities.TwinAssist
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Effects;
    using HitDelivery;

    /// <summary>
    /// Hidden twin-boss reaction: the brother strikes every enemy of the owner with DIRECT hits
    /// (no attack pipeline — reactions must not chain reactions), each hit adds a Burning stack
    /// ticking off the hit damage. Never learnable: only the reactions driver casts it.
    /// </summary>
    public class TwinAssistAttack(AbilityBaseData data) : DamagingAbility(data)
    {
        public int BurnDuration => (int)this[Parameters.BurnDuration];

        /// <summary>Global delivery by design — the assist punishes the whole opposing side.</summary>
        public IHitSequenceStrategy HitSequence { get; set; } = new AllEnemiesHits();

        public static class Parameters
        {
            public const string BurnDuration = nameof(BurnDuration);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterAppliedDuration(Parameters.BurnDuration, 4);
        }

        public override IAbility Copy() => CopyUpgradesTo(new TwinAssistAttack(Data));

        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            float damage = Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale);
            foreach (IFightable target in HitSequence.GetHitSequence(owner, targets, field))
            {
                if (!target.IsAlive) continue;

                var context = new DamageContext { Source = owner, Cause = DamageCause.Ability, CastId = CastId };
                context.Add(DamageType.Physical, damage);
                await target.TakeDamage(context);

                await ApplyBurning(owner, target, damage);
                // Direct hits by design (the class doc): the assist must not chain reactions, so it never
                // enters the attack pipeline and its touches are hits rather than attacks.
                await ApplyImpactRiders(new AbilityImpact(owner, target, field, Succeeded: true, IsCritical: false, context.TotalDamage)
                {
                    Source = this,
                    Kind = ImpactKind.Hit
                });
            }
        }

        private async Task ApplyBurning(IFightable owner, IFightable target, float hitDamage)
        {
            var context = new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId, Damage = hitDamage };
            await new DamageOverTurnEffect(BurnDuration, StatusEffects.Burning).Apply(context);
        }
    }
}
