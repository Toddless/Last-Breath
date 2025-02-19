namespace Playground
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Playground.Components;
    using Playground.Script.Effects.Interfaces;

    public interface ICharacter
    {

        HealthComponent? HealthComponent
        {
            get; set;
        }

        AttackComponent? AttackComponent
        {
            get; set;
        }

        EffectManager? EffectManager { get; }

        ObservableCollection<IAbility>? AppliedAbilities { get; set; }

        List<IAbility>? Abilities { get; }
    }
}
