namespace Core.Ai.World.Skirmish
{
    using System.Collections.Generic;

    /// <summary>
    /// Every NPC currently living in the world. NPC senses scan it for hostile neighbours
    /// (vision, skirmish initiation) — no physics layers involved.
    /// </summary>
    public interface INpcWorldRegistry
    {
        IReadOnlyList<ISkirmishParticipant> All { get; }

        void Register(ISkirmishParticipant npc);
        void Unregister(ISkirmishParticipant npc);
    }
}
