namespace Battle.Source.Abilities
{
    using Utilities;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using System.Threading.Tasks;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Godot;

    public class IncreasingPressure(
        string[] tags,
        int costValue,
        int cooldown,
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery? mastery = null,
        int maxTargets = 1,
        Costs costType = Costs.Mana)
        : Ability(id: "Ability_Increasing_Pressure", tags, cooldown, costValue, maxTargets, effects, casterEffects, upgrades, mastery, costType)
    {
        protected int HitAmounts { get; } = 5;
        protected float IncreaseHitDamageStep { get; } = 0.2f;

        public override async Task Activate(List<IEntity> targets)
        {
            await base.Activate(targets);
            await PerformAttacks(targets);
        }

        private async Task PerformAttacks(List<IEntity> targets)
        {
            if (Owner == null) return;
            var rnd = new RndGodot();
            rnd.Randomize();
            foreach (IEntity target in targets)
            {
                float increase = 1f;
                // TODO: Can this ability have more then 5 attacks??
                for (int i = 0; i < HitAmounts; i++)
                {
                    if (!target.IsAlive) return;
                    float damage = Owner.GetDamage();
                    damage += Owner.Parameters.SpellDamage;
                    damage *= increase;
                    float chance = rnd.RandFloat();
                    bool isCrit = chance <= Owner.Parameters.CriticalChance;
                    if (isCrit)
                        damage *= Owner.Parameters.CriticalDamage;
                    await target.TakeDamage(Owner, damage, DamageType.Normal, DamageSource.Ability, isCrit);
                    // TODO: this increase can have different values??
                    increase += IncreaseHitDamageStep;
                }
            }
        }

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, HitAmounts, IncreaseHitDamageStep * 100);
    }
}
