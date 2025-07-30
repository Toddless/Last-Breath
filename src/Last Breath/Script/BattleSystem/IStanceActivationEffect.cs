namespace LastBreath.Script.BattleSystem
{
    using LastBreath.Script;

    public interface IStanceActivationEffect
    {
        void OnActivate(ICharacter owner);
        void OnDeactivate(ICharacter owner);
    }
}
