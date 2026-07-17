namespace Core.Entity
{
    using System.Collections.Generic;
    using Ai;

    public interface IFightableNpc : IFightable, INpc
    {
        INpcModifiersComponent NpcModifiers { get; }

        /// <summary>Combat AI archetype. Null = no brain: the legacy basic-attack turn.</summary>
        IBehaviorProfile? Behavior { get; set; }

        /// <summary>Combat reactions from the definition (hidden triggered casts). Default empty:
        /// only bodies that applied a definition with reactions override it.</summary>
        IReadOnlyList<Data.NpcData.NpcReactionConfig> Reactions => [];

        /// <summary>Boss stages from the definition; the arena's stages controller reads them.
        /// Default empty: only bodies that applied a staged definition override it.</summary>
        IReadOnlyList<Data.NpcData.NpcStageConfig> Stages => [];

        /// <summary>Index into <see cref="Stages"/> the NPC currently fights in.</summary>
        int CurrentStageIndex => 0;

        /// <summary>Applies a stage: definition-base parameters × stage multiplier plus the stage's
        /// ability set. Default no-op keeps stage-less implementations untouched.</summary>
        void ApplyStage(int stageIndex)
        {
        }

        /// <summary>Battle-scoped summon (bone wolves and kin): gives no loot, no experience, no
        /// corpse and never returns to the world. Default false: only summon-spawned bodies override.</summary>
        bool IsSummon => false;

        float RisingBonus { get; }
        bool IsRisen { get; }
    }
}
