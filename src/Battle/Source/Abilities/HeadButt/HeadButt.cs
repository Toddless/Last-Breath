namespace Battle.Source.Abilities.HeadButt
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Enums;
    using Effects;

    /// <summary>
    /// Single lunge that stuns the target on a successful hit. L3 turns it into two lunges;
    /// impact riders (L2 armor debuff) fire per attack like in every attack-series ability.
    /// </summary>
    public class HeadButt(AbilityBaseData data) : DamagingAbility(data)
    {
        public int StunDuration => (int)this[Parameters.StunDuration];
        public int Attacks => (int)this[Parameters.Attacks];

        public static class Parameters
        {
            public const string StunDuration = nameof(StunDuration);
            public const string Attacks = nameof(Attacks);
        }

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            parameters.RegisterDefault(Parameters.StunDuration, 1);
            parameters.RegisterDefault(Parameters.Attacks, 1);
        }

        public override IAbility Copy() => CopyUpgradesTo(new HeadButt(Data));


        protected override async Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field)
        {
            foreach (IFightable target in targets)
            {
                var window = new AttackSeriesWindow(this, owner, field);
                for (int i = 0; i < Attacks; i++)
                {
                    if (!target.IsAlive) break;
                    float additionalDamage = Damage + (owner.Parameters.Damage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale);
                    var context = new AttackContext(owner, target, owner.Parameters.Damage, CombatRandom.Attacks!, window.Scheduler)
                    {
                        RawCriticalChance = owner.Parameters.CriticalChance,
                        RawCriticalDamage = owner.Parameters.CriticalDamage,
                        Index = i,
                        TotalCount = Attacks,
                        SourceAbilityId = Id
                    };
                    context.AddDamage(DamageType.Physical, additionalDamage);

                    // Every successful owner lunge stuns its target (extra attacks from reactions included).
                    bool resolved = await window.ResolveAsync(context, async processed =>
                    {
                        if (processed.Attacker.InstanceId == owner.InstanceId && processed.Result is AttackResults.Succeed)
                            await ApplyStun(owner, processed.Target);
                        return true;
                    });
                    if (!resolved) break;
                }
            }
        }

        private async Task ApplyStun(IFightable owner, IFightable target) =>
            await new StunEffect(StunDuration).Apply(new EffectApplyingContext { Caster = owner, Target = target, Source = InstanceId });
    }
}
