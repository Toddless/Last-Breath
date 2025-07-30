namespace LastBreath.Components.Interfaces
{
    using LastBreath.Script.Abilities.Interfaces;

    public interface IAbilityDecision
    {
        IAbility Ability { get; set; }
        float Priority { get; set; }
    }
}