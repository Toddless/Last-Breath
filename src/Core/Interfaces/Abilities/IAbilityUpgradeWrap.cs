namespace Core.Interfaces.Abilities
{
    using System;

    public interface IAbilityUpgradeWrap<in T> : IAbilityUpgrade
        where T : IAbility
    {
        void RemoveUpgrade(T ability);
        void ApplyUpgrade(T ability);

        IAbilityUpgrade Clone();
    }
}
