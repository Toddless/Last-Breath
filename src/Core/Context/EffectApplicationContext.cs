namespace Core.Context
{
    using Battle.Abilities;
    using Entity;

    public record EffectApplicationContext(IFightable Caster, IFightable Target, IEffect Effect) : IEffectApplicationContext
    {
        public int BonusStacks { get; set; }
    }
}
