namespace Playground.Script.Passives
{
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;

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
        public EffectType EffectType
        {
            get;
            set;
        }
        GlobalRarity Rarity
        {
            get; set;
        }
        IGameComponent? TargetComponent
        {
            get;
        }
        public void AfterBuffEnds();
        public void ActivateAbility();
        public void EffectAfterAttack();
    }
}
