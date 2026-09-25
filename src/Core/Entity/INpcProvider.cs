namespace Core.Entity
{
    using System.Collections.Generic;
    using Data.NpcData;

    /// <summary>
    /// Source of NPC definitions (Npc.json + NpcBehavior.json). CreateDefinition rolls the
    /// per-instance randomness: stance (the behavior archetype follows it), level and abilities.
    /// </summary>
    public interface INpcProvider
    {
        IReadOnlyCollection<string> KnownNpcIds { get; }
        NpcDefinition CreateDefinition(string npcId);

        /// <summary>Save-load path: pins the identity facts in <paramref name="overrides"/> instead of rolling them.</summary>
        NpcDefinition CreateDefinition(string npcId, NpcDefinitionOverrides? overrides);
    }
}
