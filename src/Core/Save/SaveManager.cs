namespace Core.Save
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Session;

    /// <param name="sessionReset">Resolved lazily: the reset list holds the save service, which holds
    /// this manager, so a direct dependency would close a DI cycle. Absent in a project that composes
    /// the save stack without session reset.</param>
    public class SaveManager(LoadScope loadScope, Func<ISessionResetService?>? sessionReset = null) : ISaveManager
    {
        private readonly List<ISaveParticipant> _participants = [];

        public event Action<string, Exception>? SectionRestoreFailed;

        public void Register(ISaveParticipant participant)
        {
            if (_participants.Any(p => p.SectionId == participant.SectionId))
                throw new ArgumentException($"Save section '{participant.SectionId}' is already registered.");
            _participants.Add(participant);
        }

        public void Unregister(string sectionId) => _participants.RemoveAll(p => p.SectionId == sectionId);

        public SaveFile Capture(SaveMetadata metadata, SaveFile? previous = null)
        {
            var file = new SaveFile { Metadata = metadata };
            if (previous != null)
                foreach ((string id, SaveSection section) in previous.Sections)
                    file.Sections[id] = section;

            foreach (var participant in _participants)
                file.Sections[participant.SectionId] = new SaveSection
                {
                    Version = participant.Version,
                    Data = participant.Capture()
                };

            return file;
        }

        public void Restore(SaveFile file)
        {
            if (file.FormatVersion > SaveFile.CurrentFormatVersion)
                throw new InvalidOperationException(
                    $"Save format {file.FormatVersion} is newer than the supported {SaveFile.CurrentFormatVersion}.");

            // A file is a whole playthrough, not a patch on the running one: every session-stateful
            // singleton goes back to its fresh-game values before the first section lands. That is what
            // makes a section the file does not carry mean "the default", instead of whatever the file
            // loaded before it left in a service that outlives the scene. Runs before the scope opens —
            // the reset carries a scope of its own and LoadScope does not nest.
            sessionReset?.Invoke()?.ResetSession();

            using var _ = loadScope.Begin();
            foreach (var participant in _participants.OrderBy(p => p.RestoreOrder))
            {
                if (!file.Sections.TryGetValue(participant.SectionId, out var section))
                {
                    RestoreMissingSection(participant);
                    continue;
                }

                if (section.Version > participant.Version)
                {
                    SectionRestoreFailed?.Invoke(participant.SectionId, new InvalidOperationException(
                        $"Section version {section.Version} is newer than the supported {participant.Version}."));
                    continue;
                }

                try
                {
                    participant.Restore(section.Data, section.Version);
                }
                catch (Exception e)
                {
                    SectionRestoreFailed?.Invoke(participant.SectionId, e);
                }
            }
        }

        /// <summary>Tells the participant the file has no section of it, reported the same way a
        /// failed restore is. Skipping it silently is what leaves the previous file's state standing
        /// in a system that outlives the scene.</summary>
        private void RestoreMissingSection(ISaveParticipant participant)
        {
            try
            {
                participant.RestoreMissingSection();
            }
            catch (Exception e)
            {
                SectionRestoreFailed?.Invoke(participant.SectionId, e);
            }
        }
    }
}
