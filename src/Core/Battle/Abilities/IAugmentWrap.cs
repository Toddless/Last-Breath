namespace Core.Battle.Abilities
{
    public interface IAugmentWrap<in T> : IAugment
        where T : IAbility
    {
        void RemoveUpgrade(T ability);
        void ApplyUpgrade(T ability);
    }
}
