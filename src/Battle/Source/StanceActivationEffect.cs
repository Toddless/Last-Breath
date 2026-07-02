namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Modifiers;

    public class StanceActivationEffect : IStanceActivationEffect
    {
        private readonly string _sourceId = Guid.NewGuid().ToString();
        private readonly List<ISkill> _passives;
        private readonly List<IModifierInstance> _modifiers = [];

        public StanceActivationEffect(List<ISkill> passives, List<IModifier> modifiers)
        {
            _passives = passives;
            foreach (IModifier modifier in modifiers)
                _modifiers.Add(new SimpleModifier(modifier.EntityParameter, modifier.ModifierValueType, modifier.Value, _sourceId));
        }

        public void ApplyModifiers(IFightable owner)
        {
            foreach (var modifier in _modifiers)
                owner.ParameterModifiers.AddModifier(modifier);
        }

        public void RemoveModifiers(IFightable owner) => owner.ParameterModifiers.RemoveModifierBySource(_sourceId);

        public void AttachPassives(IFightable owner)
        {
            foreach (ISkill passive in _passives)
                passive.Attach(owner);
        }

        public void DetachPassives(IFightable owner)
        {
            foreach (ISkill passive in _passives)
                passive.Detach(owner);
        }
    }
}
