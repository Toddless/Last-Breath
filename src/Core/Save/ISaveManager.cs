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

        /// <summary>
        /// Snapshots the registered participants into a save file: it holds their sections and nothing
        /// else, so a section whose participant is no longer registered is gone with the next save
        /// instead of being copied forward. A capture failure throws — combined with the atomic write
        /// in storage the previous file stays intact.
        /// </summary>
        SaveFile Capture(SaveMetadata metadata);

        /// <summary>Resets the session to its fresh-game state, then applies the file inside a load
        /// scope, participants ordered by RestoreOrder. The reset is the baseline the sections are
        /// deltas on: a section the file does not carry leaves the fresh value standing, not the state
        /// of the file loaded before it. A section this build cannot apply is reported and then treated
        /// as one the file never carried, so damaged data costs its own section and nothing else.</summary>
        void Restore(SaveFile file);
    }
}
