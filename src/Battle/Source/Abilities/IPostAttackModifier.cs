namespace Battle.Source.Abilities
{
    using System.Threading.Tasks;
    using Core.Interfaces.Battle;

    public interface IPostAttackModifier
    {
        Task Apply(IAttackContext context);
    }
}
