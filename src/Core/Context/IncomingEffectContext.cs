namespace Core.Context
{
    using Battle.Abilities;
    using Entity;

    public record IncomingEffectContext(IFightable Caster, IFightable Target, IEffect Effect) : IIncomingEffectContext
    {
        public bool Rejected { get; set; }
    }
}
