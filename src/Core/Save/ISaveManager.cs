namespace Core.Save
{
    using System;

    public interface ISaveManager
    {
        /// <summary>A section failed to restore (corrupt data, participant threw, or the section
        /// was written by a newer game version). The load continues with the other sections.</summary>
        event Action<string, Exception>? SectionRestoreFailed;

        void Register(ISaveParticipant participant);
        void Unregister(string sectionId);

        /// <summary>
        /// Snapshots all registered participants into a save file. Sections of <paramref name="previous"/>
        /// not owned by any registered participant are carried over untouched (other app modules'
        /// data must survive a rewrite). A capture failure throws — combined with the atomic write
        /// in storage the previous file stays intact.
        /// </summary>
        SaveFile Capture(SaveMetadata metadata, SaveFile? previous = null);

        /// <summary>Applies the file inside a load scope, participants ordered by RestoreOrder.
        /// Missing sections are skipped; failing ones are reported and skipped.</summary>
        void Restore(SaveFile file);
    }
}
