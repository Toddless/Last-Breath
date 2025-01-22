namespace Playground.Script.Passives.Attacks
{
    public interface ICharacter
    {
        HealthComponent HealthComponent
        {
            get; set;
        }

        AttackComponent AttackComponent
        {
            get; set;
        }
    }
}
