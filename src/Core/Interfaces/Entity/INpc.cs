namespace Core.Interfaces.Entity
{
    using Enums;

    public interface INpc
    {
        EntityType EntityType { get; set; }
        Fractions Fraction { get; set; }
    }
}
