namespace Core.Data
{
    using Entity;
    using Enums;

    public record DotTick(float Damage, StatusEffects Status, string Source, IFightable From);
}
