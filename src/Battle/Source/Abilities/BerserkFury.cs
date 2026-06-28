namespace Battle.Source.Abilities
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;
    using Godot;

    public class BerserkFury(
        string[] tags,
        int cooldown,
        int costValue,
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        Costs costType = Costs.Mana) : Ability(id: "Ability_Berserk_Fury", tags, cooldown, costValue, damage, weaponDamageScale, spellDamageScale, costType)
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

        public override IAbility Copy()
        {
            var copy = new BerserkFury(Tags, (int)Cooldown, CostValue, Damage, WeaponDamageScale, SpellDamageScale,  CostType);
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
        }
    }
}
