namespace Battle.Source.PassiveSkills
{
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;

    /// <summary>Item-grant passive (Ice Mage's Armor): the owner's critical attacks drop an ice
    /// meteor on the target — flat cold damage, no extra effects (yet).</summary>
    public class IceMeteorPassiveSkill(float damage) : Skill(id: "Passive_Skill_Ice_Meteor")
    {
        public float Damage { get; } = damage;

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

        public override ISkill Copy() => new IceMeteorPassiveSkill(Damage);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not IceMeteorPassiveSkill other) return false;
            return Damage > other.Damage;
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Owner == null || evt.Context.Result is not AttackResults.Succeed) return;
            if (!evt.Context.IsCritical && !evt.Context.ForceCriticalAttack) return;
            if (!evt.Context.Target.IsAlive) return;

            var meteor = new DamageContext { Source = Owner, Cause = DamageCause.Passive };
            meteor.Add(DamageType.Cold, Damage);
            _ = evt.Context.Target.TakeDamage(meteor);
        }
    }
}
