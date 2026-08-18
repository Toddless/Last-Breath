namespace Battle.Source.Abilities.JarOfPoison
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

    public class JarOfPoison(AbilityBaseData data) : DamagingAbility(data)
    {
        public int PoisonDuration => (int)this[AbilityParameter.PoisonDuration];

        /// <summary>How the jar reaches its victims: the selected target, N bounces or every enemy (L3 upgrades swap it).</summary>
        public IHitSequenceStrategy HitSequence { get; set; } = new SelectedTargetsHits();

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterAppliedDuration(AbilityParameter.PoisonDuration, 3);
            // One jar per cast, declared so the count is a thing an augment can raise: the ability wears
            // the 'projectile' tag, and a tag that promises fitting without a key to move is a purchase
            // that does nothing.
            parameters.RegisterDefault(AbilityParameter.ProjectileCount, 1);
        }

        public override IAbility Copy() => CopyUpgradesTo(new JarOfPoison(Data));

        /// <summary>One throw per counted jar, each landing on the whole hit sequence: a landing applies a
        /// poison stack AND fires the impact riders, so more jars are more stacks and more rider work by
        /// arithmetic. The landings stay HITS however many jars are thrown — that is what the jar's
        /// touches have always been, and the count is what changed.</summary>
        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            int jars = (int)this[AbilityParameter.ProjectileCount];
            for (int jar = 0; jar < jars; jar++)
                foreach (IFightable target in HitSequence.GetHitSequence(owner, targets, field))
                {
                    if (!target.IsAlive) continue;
                    await ApplyPoison(owner, target);
                    await ApplyImpactRiders(new AbilityImpact(owner, target, field)
                    {
                        Source = this,
                        Kind = ImpactKind.Hit
                    });
                }
        }

        private async Task ApplyPoison(IFightable owner, IFightable target)
        {
            float damage = Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale);

            // The jar deals no blow of its own: the poison feeds on the figure the ability authors.
            EffectApplyingContext context = Laying(target) with { Damage = DamageSnapshot.Of(DamageType.Poison, damage) };
            var poison = new DamageOverTurnEffect(PoisonDuration, StatusEffects.Poison);
            await poison.Apply(context);
        }
    }
}
