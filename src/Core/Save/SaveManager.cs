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

        public SaveFile Capture(SaveMetadata metadata)
        {
            var file = new SaveFile { Metadata = metadata };
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
                RestoreSection(participant, file);
        }

        /// <summary>
        /// Hands the participant its section, or — when the file carries nothing this build can apply
        /// for it — the fresh-game state. An absent section, one whose data throws while being read and
        /// one written by a newer build all end in the same place: the difference between them is what
        /// gets reported, not what the participant is left holding. Leaving the participant untouched
        /// instead is only harmless while a session reset can hand back the fresh value; a section
        /// owning scene state has nothing behind it, and skipping it lands the load in a world the
        /// restore never populated.
        /// </summary>
        private void RestoreSection(ISaveParticipant participant, SaveFile file)
        {
            if (!file.Sections.TryGetValue(participant.SectionId, out var section))
            {
                RestoreWithoutSection(participant);
                return;
            }

            if (section.Version > participant.Version)
            {
                Report(participant, new InvalidOperationException(
                    $"Section version {section.Version} is newer than the supported {participant.Version}."));
                RestoreWithoutSection(participant);
                return;
            }

            try
            {
                participant.Restore(section.Data, section.Version);
            }
            catch (Exception e)
            {
                Report(participant, e);
                RestoreWithoutSection(participant);
            }
        }

        /// <summary>Tells the participant this file gives it nothing to restore. Its own failure is
        /// reported the same way a failed section is — there is no third fallback behind it.</summary>
        private void RestoreWithoutSection(ISaveParticipant participant)
        {
            try
            {
                participant.RestoreWithoutSection();
            }
            catch (Exception e)
            {
                Report(participant, e);
            }
        }

        private void Report(ISaveParticipant participant, Exception e) =>
            SectionRestoreFailed?.Invoke(participant.SectionId, e);
    }
}
