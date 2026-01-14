namespace Core.Interfaces.Abilities
{
    using Enums;

    public interface IAbilityCost
    {
        public Costs Type { get; }
        public float Value { get; }
    }
}
