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

    /// <summary>
    /// Ability casts deal extra damage equal to <c>rate</c> (0.15 = 15%) of the mana
    /// the owner has spent this turn. The bonus lands ONCE per cast (keyed by CastId, on its first
    /// damaging hit); the spent pool keeps growing through the turn — every mana drop of the owner
    /// counts as spent (casts, Overload self-burn) and resets at the owner's turn start.
    /// </summary>
    public class ManaResonancePassiveSkill : Skill
    {
        private readonly ResonanceBonus _modifier;
        private float _lastMana;
        private float _spentThisTurn;
        private string? _lastBoostedCastId;

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(Rate)] = Rate,
                };
                return field;
            }
        }

        public float Rate { get; }

        public ManaResonancePassiveSkill(float rate) : base(id: "Passive_Skill_Mana_Resonance")
        {
            Rate = rate;
            _modifier = new ResonanceBonus(this);
        }


        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _lastMana = owner.CurrentMana;
            _spentThisTurn = 0;
            _lastBoostedCastId = null;
            owner.CurrentManaChanged += OnManaChanged;
            owner.CombatEvents.Subscribe<TurnStartEvent>(OnTurnStart);
            owner.ModifierHandler.Add(_modifier);
        }

        private void OnManaChanged(float currentMana)
        {
            if (currentMana < _lastMana) _spentThisTurn += _lastMana - currentMana;
            _lastMana = currentMana;
        }

        private void OnTurnStart(TurnStartEvent evt)
        {
            _spentThisTurn = 0;
            _lastMana = Owner?.CurrentMana ?? 0;
            _lastBoostedCastId = null;
        }

        public override void Detach(IFightable owner)
        {
            owner.CurrentManaChanged -= OnManaChanged;
            owner.CombatEvents.Unsubscribe<TurnStartEvent>(OnTurnStart);
            owner.ModifierHandler.Remove(_modifier);
            Owner = null;
        }

        public override ISkill Copy() => new ManaResonancePassiveSkill(Rate);

        public override bool IsStronger(ISkill skill) =>
            skill is ManaResonancePassiveSkill resonance && Rate > resonance.Rate;

        /// <summary>Outgoing mutator: adds the flat resonance bonus to the first hit of each cast,
        /// scaling the existing components proportionally so the damage-type split is preserved.</summary>
        private sealed class ResonanceBonus(ManaResonancePassiveSkill skill)
            : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Mana_Resonance"), IDamageModifier
        {
            public void Apply(IDamageContext context)
            {
                if (skill.Owner == null || skill._spentThisTurn <= 0) return;
                if (!context.Source.IsSame(skill.Owner.InstanceId)) return;
                if (context.Cause is not DamageCause.Ability) return;
                if (context.CastId == null || context.CastId == skill._lastBoostedCastId) return;
                if (context.TotalDamage <= 0) return;

                float bonus = skill.Rate * skill._spentThisTurn;
                float factor = (context.TotalDamage + bonus) / context.TotalDamage;
                foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                    context.Set(type, damage * factor);
                skill._lastBoostedCastId = context.CastId;
            }
        }
    }
}
