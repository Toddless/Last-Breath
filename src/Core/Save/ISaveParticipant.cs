namespace Core.Save
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A system owning one section of the save file. Sections store stable Ids + dynamic deltas,
    /// never live objects: loading re-runs the normal creation pipeline (provider/factory) and
    /// applies the saved state on top.
    /// </summary>
    public interface ISaveParticipant
    {
        /// <summary>Unique section key inside the save file (e.g. "mastery", "inventory").</summary>
        string SectionId { get; }

        /// <summary>Current section format version; stored with the data for per-section migrations.</summary>
        int Version { get; }

        /// <summary>Restore ordering (see <see cref="RestoreOrder"/>): modifier sources before
        /// consumers, vitals last (their setters clamp against settled maximums).</summary>
        int RestoreOrder { get; }

        JToken Capture();

        /// <summary>Applies a previously captured section. <paramref name="savedVersion"/> is the
        /// version the data was written with — older versions are migrated by the participant.</summary>
        void Restore(JToken data, int savedVersion);

        /// <summary>
        /// Applies "the file carries no section of mine" — a file written before the section existed,
        /// or by a build that did not have it. Does nothing by default: a system whose state lives on
        /// the objects other sections rebuild has nothing left to do. A system that holds its state
        /// itself and outlives the scene (a singleton) overrides this to return to the fresh-game
        /// state, because the alternative is the previous file's state silently surviving the file
        /// that replaced it — and being written back into it by the next save.
        /// </summary>
        void RestoreMissingSection()
        {
        }
    }
}
