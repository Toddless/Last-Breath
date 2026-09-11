namespace Core.Save
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Session;

    /// <param name="sessionReset">Resolved lazily to avoid a DI cycle (it holds the save service, which
    /// holds this manager). Absent in a project that composes the save stack without session reset.</param>
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

            // A file is a whole playthrough, not a patch: every session-stateful singleton resets to
            // fresh-game values first, so a missing section means "default", not a prior load's leftover.
            // Runs before the scope opens — the reset has its own scope, and LoadScope does not nest.
            sessionReset?.Invoke()?.ResetSession();

            using var _ = loadScope.Begin();
            foreach (var participant in _participants.OrderBy(p => p.RestoreOrder))
                RestoreSection(participant, file);
        }

        /// <summary>Hands the participant its section, or — when the file has nothing usable for it
        /// (absent, unreadable, or from a newer build) — the fresh-game state via RestoreWithoutSection;
        /// only the reported reason differs. Must actively hand back that state rather than leave the
        /// participant untouched: scene-owned data has no session reset to fall back on.</summary>
        private void RestoreSection(ISaveParticipant participant, SaveFile file)
        {
            if (!file.Sections.TryGetValue(participant.SectionId, out var section))
            {
                RestoreWithoutSection(participant);
                return;
            }

            if (section.Version > participant.Version)
            {
                var error = new InvalidOperationException(
                    $"Section version {section.Version} is newer than the supported {participant.Version}.");
                Report(participant, error);
                if (participant.RequiredForLoad) throw error;
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
                if (participant.RequiredForLoad) throw;
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
                if (participant.RequiredForLoad) throw;
            }
        }

        private void Report(ISaveParticipant participant, Exception e) =>
            SectionRestoreFailed?.Invoke(participant.SectionId, e);
    }
}
