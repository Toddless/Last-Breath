namespace LastBreath.Script.BattleSystem
{
    using System.Collections.Generic;
    using LastBreath.Script;
    using LastBreath.Script.Abilities.Modifiers;

    public class StanceActivationEffect : IStanceActivationEffect
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
