namespace Battle.Source.Abilities.JarOfPoison
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Effects;
    using HitDelivery;

    public class JarOfPoison(AbilityBaseData data) : DamagingAbility(data)
    {
        public int PoisonDuration => (int)this[Parameters.PoisonDuration];

        /// <summary>How the jar reaches its victims: the selected target, N bounces or every enemy (L3 upgrades swap it).</summary>
        public IHitSequenceStrategy HitSequence { get; set; } = new SelectedTargetsHits();

        public static class Parameters
        {
            public const string PoisonDuration = nameof(PoisonDuration);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.PoisonDuration, 3);
        }

        public override IAbility Copy() => CopyUpgradesTo(new JarOfPoison(Data));

        /// <summary>Each landing applies a poison stack AND fires the impact riders — every touched target gets the L2 debuffs.</summary>
        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            foreach (IFightable target in HitSequence.GetHitSequence(owner, targets, field))
            {
                if (!target.IsAlive) continue;
                await ApplyPoison(owner, target);
                await ApplyImpactRiders(new AbilityImpact(owner, target, field));
            }
        }

        private async Task ApplyPoison(IFightable owner, IFightable target)
        {
            float damage = Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale);

            var context = new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId, Damage = damage };
            var poison = new DamageOverTurnEffect(PoisonDuration, StatusEffects.Poison);
            await poison.Apply(context);
        }
    }
}
