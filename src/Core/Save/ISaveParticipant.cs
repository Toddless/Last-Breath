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
        bool RequiredForLoad => false;

        /// <summary>Current section format version; stored with the data for per-section migrations.</summary>
        int Version { get; }

        /// <summary>Restore ordering (see <see cref="RestoreOrder"/>): modifier sources before
        /// consumers, vitals last (their setters clamp against settled maximums).</summary>
        int RestoreOrder { get; }

        JToken Capture();

        /// <summary>Applies a previously captured section. <paramref name="savedVersion"/> is the
        /// version the data was written with — older versions are migrated by the participant.</summary>
        void Restore(JToken data, int savedVersion);

        /// <summary>Runs when the section is missing, unreadable, or from a newer build. No-op by default
        /// since <see cref="ISaveManager.Restore"/> already resets session-owned state first; override for
        /// scene-owned state the reset can't reach. Must set the fresh-game value outright (also runs after
        /// a <see cref="Restore"/> that threw partway through).</summary>
        void RestoreWithoutSection()
        {
        }
    }
}
