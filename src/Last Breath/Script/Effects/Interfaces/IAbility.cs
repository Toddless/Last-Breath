namespace Playground.Script.Effects.Interfaces
{
    using System;
    using System.Collections.Generic;
    using Playground.Script.Passives.Attacks;

    public interface IAbility
    {
        int Cooldown
        {
            get; set;
        }

        Action<ICharacter, IAbility> OnReceiveAbilityHandler { get; set; }

        List<IEffect> Effects { get; set; }

        void ActivateAbility(ICharacter character);
    }
}
