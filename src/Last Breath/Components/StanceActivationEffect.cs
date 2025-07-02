namespace Playground.Components
{
    using System.Collections.Generic;
    using Playground.Script;
    using Playground.Script.Abilities.Modifiers;

    public class StanceActivationEffect
    {
        private List<IModifier> _modifiers = [];

        public StanceActivationEffect()
        {
            LoadData();
        }

        public void OnActivate(ICharacter owner)
        {
            foreach (IModifier modifier in _modifiers)
            {
                owner.Modifiers.AddPermanentModifier(modifier);
            }
        }

        public void OnDeactivate(ICharacter owner)
        {
            owner.Modifiers.RemovePermanentModifierBySource(this);
        }

        private void LoadData()
        {

        }
    }
}
