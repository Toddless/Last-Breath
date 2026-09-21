namespace Core.World.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>What a restore finds wrong with a saved chest state.</summary>
    public enum ChestStateProblem
    {
        /// <summary>The state was saved for another definition than the one the chest is placed with.</summary>
        DefinitionChanged,

        /// <summary>A slot held a quantity of an item that could not be read back.</summary>
        ItemMissing,

        /// <summary>An opened chest with nothing left carried no removal deadline.</summary>
        DeadlineMissing,

        /// <summary>The state was written under a version this build does not read.</summary>
        VersionNotRead,

        /// <summary>The state cannot be read as a chest.</summary>
        Unreadable
    }

    /// <summary>One problem a restore found, described for the report.</summary>
    public record ChestStateFinding(ChestStateProblem Problem, string Description);

    /// <summary>A chest state as a save holds it, its items read back: a slot's item is null where it could not be.</summary>
    public record SavedChestState(string DefinitionId, bool Initialized, IReadOnlyList<ChestSlot> Slots, double? RemoveAtMinutes);

    /// <summary>A state a chest can hold, in the shape <see cref="ChestContents.Restore"/> takes.</summary>
    public record RestoredChestState(bool Initialized, List<ChestSlot> Slots, double? RemoveAtMinutes);

    /// <summary>The state to restore, or null when the save gives none and the chest stays shut, with what was found on the way.</summary>
    public record ChestStateRepairResult(RestoredChestState? State, IReadOnlyList<ChestStateFinding> Findings);

    /// <summary>Makes a saved chest state into one the chest can hold: content changed since the save is repaired and reported,
    /// and a state that cannot be read is refused whole, so nothing is minted in its place.</summary>
    public static class ChestStateRepair
    {
        /// <summary>The removal delay of a chest whose definition the catalog no longer holds.</summary>
        private const double UnknownDefinitionDelay = 0d;

        /// <summary>What a slot keeps once its item could not be read back.</summary>
        private const int NothingLeft = 0;

        private const string DefinitionChangedFormat =
            "it was saved for definition '{0}' and the chest is placed with '{1}'; the saved slots are restored as they are";

        private const string ItemMissingFormat = "slot '{0}' held {1} of an item that can no longer be rebuilt; the slot is left empty";

        private const string DeadlineMissingFormat =
            "the opened chest has nothing left and no removal deadline was saved; it is removed at {0:0.##} game minutes";

        private const string VersionNotReadFormat = "it was written under state version {0}, and this build reads version {1}";

        private const string UnreadableFormat = "it cannot be read as a chest state: {0}";

        private const string IncoherentStateProblem = "its slots and removal deadline do not form a state a chest can hold";

        /// <summary>The chest state version this build writes and reads.</summary>
        public const int StateVersion = 1;

        /// <summary>True when this build reads a state written under <paramref name="version"/>.</summary>
        public static bool Reads(int version) => version == StateVersion;

        /// <summary>The refusal of a state written under a version this build does not read.</summary>
        public static ChestStateRepairResult VersionNotRead(int version) =>
            Refuse(ChestStateProblem.VersionNotRead, VersionNotReadFormat, version, StateVersion);

        /// <summary>The refusal of a state that cannot be read, naming its <paramref name="problem"/>.</summary>
        public static ChestStateRepairResult Unreadable(string problem) => Refuse(ChestStateProblem.Unreadable, UnreadableFormat, problem);

        /// <summary>Empties every slot whose item could not be read back and gives an opened chest left with nothing the deadline it
        /// lacks. The saved slots are kept under another definition too; a state still incoherent after that is refused whole.</summary>
        /// <param name="placedDefinitionId">The definition the chest is placed with.</param>
        /// <param name="now">The current game time, in game minutes.</param>
        /// <param name="removalDelayMinutes">The placed definition's removal delay; null when the catalog holds no such definition.</param>
        public static ChestStateRepairResult Repair(SavedChestState saved, string placedDefinitionId, double now, double? removalDelayMinutes)
        {
            List<ChestSlot> slots = [.. saved.Slots.Select(EmptiedIfItemless)];
            var removeAt = saved.RemoveAtMinutes ?? DeadlineIfEmpty(saved.Initialized, slots, now, removalDelayMinutes);
            return ChestContents.IsCoherent(saved.Initialized, slots, removeAt)
                ? new(new RestoredChestState(saved.Initialized, slots, removeAt), Findings(saved, placedDefinitionId, removeAt))
                : Unreadable(IncoherentStateProblem);
        }

        private static ChestSlot EmptiedIfItemless(ChestSlot slot) => slot.LacksItem ? new ChestSlot(slot.Id, null, NothingLeft) : slot;

        /// <summary>The deadline an opened chest with nothing left starts at: now plus the definition's delay.</summary>
        private static double? DeadlineIfEmpty(bool initialized, IReadOnlyList<ChestSlot> slots, double now, double? removalDelayMinutes) =>
            ChestContents.IsEmpty(initialized, slots) ? now + (removalDelayMinutes ?? UnknownDefinitionDelay) : null;

        /// <summary>One finding per repair the saved state needed.</summary>
        private static List<ChestStateFinding> Findings(SavedChestState saved, string placedDefinitionId, double? removeAt) =>
        [
            .. DefinitionChange(saved.DefinitionId, placedDefinitionId),
            .. saved.Slots.Where(slot => slot.LacksItem).Select(ItemMissing),
            .. DeadlineAdded(saved.RemoveAtMinutes, removeAt)
        ];

        private static IEnumerable<ChestStateFinding> DefinitionChange(string savedId, string placedId) =>
            string.Equals(savedId, placedId, StringComparison.Ordinal)
                ? []
                : [Found(ChestStateProblem.DefinitionChanged, DefinitionChangedFormat, savedId, placedId)];

        private static ChestStateFinding ItemMissing(ChestSlot slot) => Found(ChestStateProblem.ItemMissing, ItemMissingFormat, slot.Id, slot.Amount);

        private static IEnumerable<ChestStateFinding> DeadlineAdded(double? saved, double? restored) =>
            saved == null && restored is { } deadline ? [Found(ChestStateProblem.DeadlineMissing, DeadlineMissingFormat, deadline)] : [];

        private static ChestStateRepairResult Refuse(ChestStateProblem problem, string format, params object?[] values) =>
            new(null, [Found(problem, format, values)]);

        private static ChestStateFinding Found(ChestStateProblem problem, string format, params object?[] values) =>
            new(problem, string.Format(CultureInfo.InvariantCulture, format, values));
    }
}
