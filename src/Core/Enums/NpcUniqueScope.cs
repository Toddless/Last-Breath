namespace Core.Enums
{
    /// <summary>How far "unique" reaches for an NPC modifier — see the uniqueScope field of a
    /// NpcModifiers.json section. Group is the default because it is what most sections mean.</summary>
    public enum NpcUniqueScope : byte
    {
        /// <summary>One unique modifier of the catalog section at a time; the stronger one takes the slot.</summary>
        Group = 0,

        /// <summary>Only the same modifier twice is refused — different ones of the section stack.</summary>
        Id,
    }
}
