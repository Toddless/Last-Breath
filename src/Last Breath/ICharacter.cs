namespace Playground
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Playground.Components;
    using Playground.Script.Effects.Interfaces;

    public interface ICharacter
    {

        HealthComponent? EnemyHealth
        {
            get; set;
        }

        DamageComponent? EnemyDamage
        {
            get; set;
        }

        ObservableCollection<IAbility>? AppliedAbilities { get; set; }

        List<IAbility>? Abilities { get; }
    }
}
