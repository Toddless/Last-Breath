namespace Core.Interfaces.Abilities
{
    using Enums;

    public interface IAbilityCost
    {
        public Costs Resource { get; }
        public int Value { get; }
    }
}
