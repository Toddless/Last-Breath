namespace LastBreath.Npc
{
    using System.Collections.Generic;
    using Core.Ai.World.Skirmish;

    /// <summary>NPCs register on ready and unregister on dispose; senses scan the list by distance.</summary>
    public class NpcWorldRegistry : INpcWorldRegistry
    {
        private readonly List<ISkirmishParticipant> _npcs = [];

        public IReadOnlyList<ISkirmishParticipant> All => _npcs;

        public void Register(ISkirmishParticipant npc)
        {
            if (!_npcs.Contains(npc)) _npcs.Add(npc);
        }

        public void Unregister(ISkirmishParticipant npc) => _npcs.Remove(npc);
    }
}
