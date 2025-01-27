namespace Playground.Script.Passives.Attacks
{
    using System.Collections.ObjectModel;
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

        ObservableCollection<IAbility>? AppliedAbilities { get; set; }
    }
}
