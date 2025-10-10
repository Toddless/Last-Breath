namespace LastBreath.Script.BattleSystem
{
    using System.Collections.Generic;
    using Core.Interfaces;
    using Core.Interfaces.Battle;

    public class StanceActivationEffect : IStanceActivationEffect
    {
        private List<IItemModifier> _modifiers = [];

        public StanceActivationEffect()
        {
        }

        public void OnActivate(ICharacter owner)
        {
            foreach (var modifier in _modifiers)
            {
                owner.Modifiers.AddPermanentModifier(modifier);
            }
        }

        public void OnDeactivate(ICharacter owner)
        {
            owner.Modifiers.RemovePermanentModifierBySource(this);
        }
    }
}
