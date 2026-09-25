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
    /// Single lunge that stuns the target on a successful hit. The lunge count is a parameter no
    /// shipped record moves; impact riders — the generic attack-tagged debuff records among them —
    /// fire per attack as in every attack-series ability.
    /// </summary>
    public class HeadButt(AbilityBaseData data) : DamagingAbility(data)
    {
        public int StunDuration => (int)this[AbilityParameter.StunDuration];
        public int Attacks => (int)this[AbilityParameter.Attacks];

        protected override void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            base.RegisterBaseParameters(parameters);
            RegisterCriticalParameters(parameters);
            parameters.RegisterDefault(AbilityParameter.AccuracyBonus, 0f);
            // Owned for what the lunge CARRIES rather than for what it lays itself: the stun is control
            // without a figure, while every debuff an augment hangs on the lunge reads the cast's
            // effectiveness — so the concept is the ability's, and a record on it is felt there.
            parameters.RegisterDefault(AbilityParameter.Effectiveness, 1f);
            parameters.RegisterAppliedDuration(AbilityParameter.StunDuration, 1);
            parameters.RegisterDefault(AbilityParameter.Attacks, 1);
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
                    float additionalDamage = Damage + (owner.Parameters.PhysicalDamage * WeaponDamageScale) + (owner.Parameters.SpellDamage * SpellDamageScale);
                    var context = new AttackContext(owner, target, owner.Parameters.PhysicalDamage, CombatRandom.Attacks!, window.Scheduler)
                    {
                        Index = i,
                        TotalCount = Attacks,
                        SourceAbilityId = Id
                    };
                    context.UseCriticalOf(this);
                    context.UseAccuracyOf(this);
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
            await new StunEffect(StunDuration).Apply(Laying(target));
    }
}
