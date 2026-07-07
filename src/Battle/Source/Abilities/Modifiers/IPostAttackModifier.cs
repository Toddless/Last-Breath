namespace Battle.Source.Abilities.Modifiers
{
    using System.Threading.Tasks;
    using Core.Battle;

    public interface IPostAttackModifier
    {
        Task Apply(IAttackContext context);
    }
}
