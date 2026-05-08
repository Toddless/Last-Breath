namespace Core.Interfaces
{
    using Enums;

    public interface IHealModifier
    {
        Priority Priority { get; set; }
        void Apply(IHealContext context);
    }
}
