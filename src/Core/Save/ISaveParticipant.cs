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
        /// or by a build that did not have it. Does nothing by default, and for most participants that
        /// is the whole answer: the restore starts by resetting the session (see
        /// <see cref="ISaveManager.Restore"/>), so a service registered as
        /// <see cref="Session.ISessionResettable"/> is already back at its fresh-game value when the
        /// section turns out to be missing.
        /// Override it where the fresh-game state is NOT the session reset's to give — state owned by
        /// the scene, which is built before the file is applied and cannot be rebuilt by resetting a
        /// singleton.
        /// </summary>
        void RestoreMissingSection()
        {
        }
    }
}
