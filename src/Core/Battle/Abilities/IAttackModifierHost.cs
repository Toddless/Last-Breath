namespace Core.Battle.Abilities
{
    /// <summary>An ability whose attacks can be tuned by a context modifier — what a data-declared
    /// attack behaviour needs, instead of one upgrade class per ability that owns a pipeline.</summary>
    public interface IAttackModifierHost
    {
        void AddAttackModifier(IAttackModifier modifier);

        void RemoveAttackModifier(string id);
    }
}
