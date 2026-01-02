namespace Battle.Source.Abilities
{
    using Godot;
    using Services;
    using TestData;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;

    public class BerserkFury(
        string[] tags,
        int cooldown,
        int costValue,
        float maxTargets,
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery? mastery = null,
        Costs costType = Costs.Mana) : Ability(id: "Ability_Berserk_Fury", tags, cooldown, costValue, maxTargets, effects, casterEffects, upgrades, mastery,
        costType)
    {
        public override async Task Activate(List<IEntity> targets)
        {
            await base.Activate(targets);
            await PerformMultipleAttacks(targets);
        }

        private async Task PerformMultipleAttacks(List<IEntity> targets)
        {
            if (Owner == null) return;
            var scheduler = new AttackContextScheduler();
            while (true)
            {
                if (Owner.CurrentHealth <= 1) break;
                foreach (IEntity target in targets)
                {
                    var context = new AttackContext(Owner, target, Owner.GetDamage(), new RndGodot(), scheduler);
                    scheduler.Schedule(context);
                    await scheduler.RunQueue();
                    if (Owner.CurrentHealth <= 1) break;
                }

                // with lower hp we have lower chance for next cycle
                float chance = Mathf.Clamp(Owner.CurrentHealth / Owner.Parameters.MaxHealth, 0.05f, 0.80f);
                if (StaticRandomNumberGenerator.Rnd.Randf() > chance)
                    break;
            }
        }
    }
}
