namespace Battle.Source.Abilities
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;

    public class BerserkFury(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        Costs costType = Costs.Mana) : Ability(id: "Ability_Berserk_Fury", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, upgrades, costType)
    {
        private async Task PerformMultipleAttacks(List<IEntity> targets)
        {
            if (Owner == null) return;
            var scheduler = new AttackContextScheduler();
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            while (true)
            {
                if (Owner.CurrentHealth <= 1) break;
                foreach (IEntity target in targets)
                {
                    var context = new AttackContext(Owner, target, Owner.GetDamage(), rnd, scheduler);
                    scheduler.Schedule(context);
                    await foreach (var processed in scheduler.RunQueue())
                    {
                    }

                    if (Owner.CurrentHealth <= 1) break;
                }

                // with lower hp we have a lower chance for the next cycle
                float chance = Mathf.Clamp(Owner.CurrentHealth / Owner.Parameters.MaxHealth, 0.05f, 0.80f);
                if (rnd.Randf() > chance)
                    break;
            }
        }

        public override IAbility Copy() => new BerserkFury(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale, Upgrades, CostType);
    }
}
