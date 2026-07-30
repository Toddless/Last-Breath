namespace Battle.Source.PassiveSkills
{
    using System.Collections.Generic;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Modifiers;

    /// <summary>
    /// Every ability used this turn grants a stack of
    /// +<c>spellDamagePerStack</c> spell damage and +<c>multicastPerStack</c> multicast chance.
    /// Stacks live until the end of the owner's turn.
    /// </summary>
    public class ResonancePassiveSkill : Skill
    {
        private readonly IModifierInstance _spellDamageModifier;
        private readonly IModifierInstance _multicastModifier;
        private int _stacks;

        protected override IReadOnlyDictionary<string, object?>? DescriptionValues
        {
            get
            {
                if (field != null) return field;
                field = new Dictionary<string, object?>
                {
                    [nameof(SpellDamagePerStack)] = SpellDamagePerStack,
                    [nameof(MulticastPerStack)] = MulticastPerStack
                };
                return field;
            }
        }

        public float SpellDamagePerStack { get; }
        public float MulticastPerStack { get; }

        public ResonancePassiveSkill(float spellDamagePerStack = 0.04f, float multicastPerStack = 0.10f)
            : base(id: "Passive_Skill_Resonance")
        {
            SpellDamagePerStack = spellDamagePerStack;
            MulticastPerStack = multicastPerStack;
            _spellDamageModifier = new SimpleModifier(EntityParameter.SpellDamage, ModifierValueType.Increase, 0f, InstanceId);
            _multicastModifier = new SimpleModifier(EntityParameter.MulticastChance, ModifierValueType.Flat, 0f, InstanceId);
        }

        public override void Attach(IFightable owner)
        {
            Owner = owner;
            _stacks = 0;
            owner.CombatEvents.Subscribe<AbilityActivatedEvent>(OnAbilityActivated);
            owner.CombatEvents.Subscribe<TurnEndEvent>(OnTurnEnd);
        }

        private void OnAbilityActivated(AbilityActivatedEvent evt)
        {
            _stacks++;
            UpdateModifiers();
        }

        private void OnTurnEnd(TurnEndEvent evt)
        {
            if (_stacks == 0) return;
            _stacks = 0;
            UpdateModifiers();
        }

        public override void Detach(IFightable owner)
        {
            owner.CombatEvents.Unsubscribe<AbilityActivatedEvent>(OnAbilityActivated);
            owner.CombatEvents.Unsubscribe<TurnEndEvent>(OnTurnEnd);
            owner.ParameterModifiers.RemoveModifier(_spellDamageModifier);
            owner.ParameterModifiers.RemoveModifier(_multicastModifier);
            _stacks = 0;
            Owner = null;
        }

        public override ISkill Copy() => new ResonancePassiveSkill(SpellDamagePerStack, MulticastPerStack);

        /// <summary>Spell damage per stack decides: it is the always-on half of the stack, while
        /// multicast chance only pays out on a roll.</summary>
        public override bool IsStronger(ISkill skill) =>
            skill is ResonancePassiveSkill resonance && SpellDamagePerStack > resonance.SpellDamagePerStack;

        private void UpdateModifiers()
        {
            if (Owner == null) return;
            _spellDamageModifier.Value = _stacks * SpellDamagePerStack;
            _multicastModifier.Value = _stacks * MulticastPerStack;
            Owner.ParameterModifiers.UpdateModifier(_spellDamageModifier);
            Owner.ParameterModifiers.UpdateModifier(_multicastModifier);
        }
    }
}
