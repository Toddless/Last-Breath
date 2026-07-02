namespace Battle.Source
{
    using System.Collections.Generic;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using Core.Modifiers;

    public class StanceActivationEffect : IStanceActivationEffect
    {
        private List<ISkill> _passives;
        private List<IModifierInstance> _modifiers = [];

        public StanceActivationEffect(List<ISkill> passives, List<IModifier> modifiers)
        {
            //TODO: Add id and instance id
            _passives = passives;
            foreach (IModifier modifier in modifiers)
                _modifiers.Add(new SimpleModifier(modifier.EntityParameter, modifier.ModifierValueType, modifier.Value, string.Empty));
        }

        public void OnActivate(IFightable owner)
        {
            foreach (var modifier in _modifiers)
                owner.ParameterModifiers.AddModifier(modifier);
            foreach (ISkill passive in _passives)
                passive.Attach(owner);
        }

        public void OnDeactivate(IFightable owner)
        {
            // TODO: Add id and instanceId later
            owner.ParameterModifiers.RemoveModifierBySource(string.Empty);
            _passives.ForEach(x => x.Detach(owner));
        }
    }
}
