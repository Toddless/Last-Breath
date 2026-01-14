namespace Core.Interfaces.Battle
{
    using System.Collections.Generic;
    using Abilities;
    using Enums;
    using Skills;

    public interface IStance
    {
        int CurrentLevel { get; }
        Stance StanceType { get; }

        IReadOnlyList<ISkill> ObtainedPassiveSkills { get; }
        IReadOnlyList<IAbility> ObtainedAbilities { get; }

        void OnActivate();
        void OnDeactivate();
    }
}
