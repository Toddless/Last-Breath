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

        void OnActivate();
        void OnDeactivate();
    }
}
