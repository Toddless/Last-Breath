namespace Playground.Script.Effects.Interfaces
{
    using System;
    using System.Collections.Generic;
    using Playground;
    using Playground.Script.Scenes;

    public interface IAbility
    {
        int Cooldown
        {
            get; set;
        }

        Action<ICharacter, IAbility> OnApplyAbilityHandler { get; set; }

        List<IEffect> Effects { get; set; }

        void ActivateAbility(IBattleContext context);
    }
}
