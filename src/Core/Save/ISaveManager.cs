namespace Core.Save
{
    using System;

    public interface ISaveManager
    {
        /// <summary>A section failed to restore (corrupt data, participant threw, or the section
        /// was written by a newer game version). The participant is then given the fresh-game state
        /// and the load continues with the other sections.</summary>
        event Action<string, Exception>? SectionRestoreFailed;

        void Register(ISaveParticipant participant);
        void Unregister(string sectionId);

        /// <summary>Snapshots registered participants only — an unregistered section is dropped, not
        /// carried forward. Throws on failure; combined with the atomic write in storage, the previous
        /// file survives.</summary>
        SaveFile Capture(SaveMetadata metadata);

        /// <summary>Resets to fresh-game state, then applies the file's sections in RestoreOrder as
        /// deltas on that baseline — a missing section just leaves the fresh value standing. A section
        /// that fails to apply is reported then treated as missing, so damage stays contained to it.</summary>
        void Restore(SaveFile file);
    }
}
