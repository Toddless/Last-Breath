namespace Battle.Source.PassiveSkills
{
    using System.Linq;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Effects;

    /// <summary>Item-grant passive (Bloodthirsty): when the target reaches the stack threshold,
    /// all remaining bleed stacks are consumed, their leftover damage lands at once
    /// and the owner is healed for a share of it.</summary>
    public class BloodthirstyPassiveSkill(int stackThreshold, float healPercent)
        : Skill(id: "Passive_Skill_Bloodthirsty")
    {
        public int StackThreshold { get; } = stackThreshold;
        public float HealPercent { get; } = healPercent;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AfterAttackEvent>(OnAfterAttack);
        }

        public override void Detach(IFightable owner)
        {
            Owner?.CombatEvents.Unsubscribe<AfterAttackEvent>(OnAfterAttack);
            Owner = null;
        }

        public override ISkill Copy() => new BloodthirstyPassiveSkill(StackThreshold, HealPercent);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not BloodthirstyPassiveSkill other) return false;
            return HealPercent > other.HealPercent;
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Owner == null || evt.Context.Result is not AttackResults.Succeed) return;
            var target = evt.Context.Target;
            var bleeds = target.Effects
                .GetBy(effect => (effect.Status & StatusEffects.Bleed) != 0)
                .OfType<DamageOverTurnEffect>()
                .ToList();
            if (bleeds.Count < StackThreshold) return;

            // A stack with duration D still has D+1 ticks ahead (the tick fires before the countdown)
            float total = bleeds.Sum(bleed => bleed.DamagePerTick * (bleed.Duration + 1));
            foreach (var bleed in bleeds) bleed.Remove();

            var damage = new DamageContext { Source = Owner, Cause = DamageCause.Passive };
            damage.Add(DamageType.Bleed, total);
            _ = target.TakeDamage(damage);

            Owner.Heal(new HealContext(Owner, Owner) { Amount = total * HealPercent, Cause = HealCause.Leech });
        }
    }
}
