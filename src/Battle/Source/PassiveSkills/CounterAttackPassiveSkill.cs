namespace Battle.Source.PassiveSkills
{
    using System;
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Events;

    public class CounterAttackPassiveSkill(float chance = 0.5f) : Skill(id: "Passive_Skill_Counter_Attack")
    {
        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(Chance)] = Chance
                };
                return field;
            }
        }

        public float Chance { get; } = chance;

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            Owner.CombatEvents.Subscribe<AttackEvadedEvent>(OnAttackEvaded);
        }

        private void OnAttackEvaded(AttackEvadedEvent evnt)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(Owner);
                if (evnt.Context.Target.InstanceId != Owner.InstanceId) return;
                if (evnt.Context.Rnd.Randf() > Chance) return;
                evnt.Context.CreateReaction(Owner, evnt.Context.Attacker, Owner.GetDamage()).Schedule();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AttackEvadedEvent>(OnAttackEvaded);
            Owner = null;
        }

        public override ISkill Copy() => new CounterAttackPassiveSkill(Chance);

        public override bool IsStronger(ISkill skill)
        {
            if (skill is not CounterAttackPassiveSkill counter) return false;
            return counter.Chance > Chance;
        }
    }
}
