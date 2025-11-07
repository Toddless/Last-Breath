namespace Battle
{
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class StanceActivationEffect : IStanceActivationEffect
    {
        private List<IModifierInstance> _modifiers = [];

        public StanceActivationEffect()
        {
        }

        public void OnActivate(IEntity owner)
        {
            foreach (var modifier in _modifiers)
            {
                owner.Modifiers.AddPermanentModifier(modifier);
            }
        }

        public void OnDeactivate(IEntity owner)
        {
            owner.Modifiers.RemovePermanentModifierBySource(this);
        }
    }
}
