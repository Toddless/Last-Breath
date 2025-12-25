namespace Core.Data
{
    using Enums;
    using Interfaces.Entity;

    public record DotTick(float Damage, StatusEffects Status, string Source, IEntity From);
}
