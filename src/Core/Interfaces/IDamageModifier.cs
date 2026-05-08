namespace Core.Interfaces
{
    using Enums;

    public interface IDamageModifier
    {
        Priority Priority { get; set; }
        void Apply(IDamageContext context);
    }
}
