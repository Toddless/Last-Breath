namespace Core.Battle.Abilities
{
    public interface IAbilityAugmentWrap<in T> : IAbilityAugment
        where T : IAbility
    {
        void RemoveUpgrade(T ability);
        void ApplyUpgrade(T ability);
    }
}
