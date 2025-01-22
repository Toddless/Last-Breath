namespace Playground.Script.Passives
{
    using System;
    using Playground.Components.Interfaces;
    using Playground.Script.Passives.Attacks;

    public interface IAbility
    {
        int BuffLasts
        {
            get; set;
        }
        int Cooldown
        {
            get; set;
        }
        Type TargetType { get; }

        IGameComponent? TargetComponent
        {
            get;
        }
        ICharacter? TargetCharacter
        {
            get;
        }
        public void SetTargetCharacter(ICharacter? character);
        public void ActivateAbility();
    }
}
