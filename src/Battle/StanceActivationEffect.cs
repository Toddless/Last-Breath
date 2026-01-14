namespace Battle
{
    using Core.Modifiers;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Skills;
    using System.Collections.Generic;

    public class StanceActivationEffect : IStanceActivationEffect
    {
        private List<ISkill> _passives;
        private List<IModifierInstance> _modifiers = [];

        public StanceActivationEffect(List<ISkill> passives, List<IModifier> modifiers)
        {
            _passives = passives;
            foreach (IModifier modifier in modifiers)
                _modifiers.Add(new ModifierInstance(modifier.EntityParameter, modifier.ModifierType, modifier.Value, this));
        }

        public void OnActivate(IEntity owner)
        {
            foreach (var modifier in _modifiers)
                owner.Modifiers.AddPermanentModifier(modifier);
            foreach (ISkill passive in _passives)
                passive.Attach(owner);
        }

        public void OnDeactivate(IEntity owner)
        {
            owner.Modifiers.RemovePermanentModifierBySource(this);
            _passives.ForEach(x => x.Detach(owner));
        }
    }
}
