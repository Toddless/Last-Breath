namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Skills;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Modifiers.Context;

    /// <summary>"Bastion": the first attack/ability hit taken after each of the
    /// owner's turn starts deals <c>reduce</c> (0.35 = −35%) less damage.</summary>
    public class BastionPassiveSkill : Skill
    {
        private readonly FirstHitTakenReduction _modifier;

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(Reduce)] = Reduce
                };
                return field;
            }
        }

        public float Reduce { get; }

        public BastionPassiveSkill(float reduce) : base(id: "Passive_Skill_Bastion")
        {
            Reduce = reduce;
            _modifier = new FirstHitTakenReduction(this);
        }


        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _modifier.Rearm();
            owner.ModifierHandler.Add(_modifier);
            owner.CombatEvents.Subscribe<TurnStartEvent>(OnTurnStart);
        }

        private void OnTurnStart(TurnStartEvent evt) => _modifier.Rearm();

        public override void Detach(IFightable owner)
        {
            owner.ModifierHandler.Remove(_modifier);
            owner.CombatEvents.Unsubscribe<TurnStartEvent>(OnTurnStart);
            Owner = null;
        }

        public override ISkill Copy() => new BastionPassiveSkill(Reduce);

        public override bool IsStronger(ISkill skill) =>
            skill is BastionPassiveSkill bastion && Reduce > bastion.Reduce;

        /// <summary>Incoming-damage gate: one charge per rearm; the owner's own dealt damage
        /// (the handler runs for both directions) never consumes it.</summary>
        private sealed class FirstHitTakenReduction(BastionPassiveSkill skill)
            : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Bastion"), IDamageModifier
        {
            private bool _armed;

            public void Rearm() => _armed = true;

            public void Apply(IDamageContext context)
            {
                if (!_armed || skill.Owner == null) return;
                if (context.Source.IsSame(skill.Owner.InstanceId)) return;
                if (context.Cause is not (DamageCause.Attack or DamageCause.Ability)) return;
                if (context.TotalDamage <= 0) return;

                _armed = false;
                foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                    context.Set(type, damage * (1 - skill.Reduce));
            }
        }
    }
}
