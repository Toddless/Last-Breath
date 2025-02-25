namespace Playground.Script.Effects.Interfaces
{
    using System;
    using System.Collections.Generic;
    using Playground.Script.ScenesHandlers;

    public interface IAbility
    {
        int Cooldown
        {
            get; set;
        }

        Action<ICharacter, IAbility> AbilityHandler { get; set; }

        List<IEffect> Effects { get; set; }

        void ActivateAbility(IBattleContext context);
    }
}
