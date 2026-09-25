namespace Core.Narrative.Facts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ai.World.Spawn;
    using Conditions;
    using Dialogues;

    /// <summary>What one place does with a fact key. Both halves are one question: a key nobody writes
    /// gates nothing, and a key nobody reads is a flag raised into the void.</summary>
    public enum FactKeyUseKind
    {
        Write,
        Read
    }

    /// <summary>One place a fact key is used: the key as it is written there, what the place does with it,
    /// and the address the place stands at — a data address such as
    /// <c>Quests/Q/stages/S/onEnter[0]/key</c>, or the code that keeps the key itself.</summary>
    public sealed record FactKeyUse(string Key, FactKeyUseKind Kind, string Where);

    /// <summary>
    /// One family of facts the game's own code keeps: the template its builder writes, the code that
    /// writes a key of the family and the code that reads one back.
    /// <para>Written out rather than reflected off the builders. A signature says what a key is spelt
    /// like; it says nothing about who raises it and who asks about it, and those two are the whole
    /// reason a registry is worth reading.</para>
    /// </summary>
    public sealed record FactKeyDeclaration
    {
        /// <summary>The family as it is written with its parameters named — <c>Npc_Talked:&lt;npcId&gt;</c>.</summary>
        public required string Template { get; init; }

        /// <summary>The code that writes a key of this family.</summary>
        public required IReadOnlyList<string> Writers { get; init; }

        /// <summary>The code that reads one back. Empty is an answer and not a gap: the data may be the
        /// only reader, and a family nothing reads at all is what the checks report.</summary>
        public IReadOnlyList<string> Readers { get; init; } = [];

        /// <summary>The head every key of the family carries, the separator included; null for a family of
        /// one, whose template IS the key.</summary>
        public string? Head
        {
            get
            {
                int separator = Template.IndexOf(FactKeys.Separator);

                return separator < 0 ? null : Template[..(separator + 1)];
            }
        }

        /// <summary>Whether a written key belongs to the family. A head with nothing after it does not:
        /// the family named without one of its members is not a member of it, and folding such a word in
        /// would answer for a key the game never keeps.</summary>
        public bool Covers(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return Head is { } head
                ? key.Length > head.Length && key.StartsWith(head, StringComparison.Ordinal)
                : string.Equals(key, Template, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Every family of facts the game's own code keeps, with the code that writes each one and the code
    /// that reads it back. The data's own keys are read out of the documents; these are the ones no
    /// document mentions, and without them every counter the quests watch would look unwritten.
    /// </summary>
    /// <remarks>A builder of <see cref="FactKeys"/> without a family here is a key the registry cannot
    /// answer for and the tool cannot offer, which is what a test holds. The code addresses are names and
    /// not places: what writes a fact lives in four projects, and only one of them is this one.</remarks>
    public static class FactKeyDeclarations
    {
        /// <summary>How an address in the code is told apart from an address in the data.</summary>
        public const string CodePrefix = "code:";

        /// <summary>How a parameter of a family is written inside its template.</summary>
        private const char ParameterOpen = '<';

        private const char ParameterClose = '>';

        private const string LocationParameter = "locationId";

        private const string NpcParameter = "npcId";

        private const string FactionParameter = "faction";

        private const string BossParameter = "bossId";

        private const string NodeParameter = "nodeId";

        private const string OptionParameter = "optionId";

        private const string QuestParameter = "questId";

        private const string PieceParameter = "piece";

        /// <summary>The world map marks a place found, and the marker in the world asks whether it already
        /// has been before publishing the event a second time.</summary>
        private const string LocationMarkerCode = "LocationMarker";

        /// <summary>The npc's reactions read a boss's final death; the spawn point that keeps the boss
        /// reads the same key to decide whether he is gone for good.</summary>
        private const string NpcReactionsCode = "NpcReactionsDriver";

        private const string BossSpawnPointCode = "BossSpawnPoint";

        public static IReadOnlyList<FactKeyDeclaration> All { get; } =
        [
            new()
            {
                Template = Family(FactKeys.LocationDiscoveredHead, LocationParameter),
                Writers = [Code(nameof(LocationFactTracker))],
                Readers = [Code(LocationMarkerCode)]
            },
            new()
            {
                Template = Family(FactKeys.KillCountHead, NpcParameter),
                Writers = [Code(nameof(KillFactTracker))]
            },
            new()
            {
                Template = Family(FactKeys.FactionKillCountHead, FactionParameter),
                Writers = [Code(nameof(KillFactTracker))]
            },
            new()
            {
                Template = Family(FactKeys.NpcFinalDeathHead, NpcParameter),
                Writers = [Code(nameof(NpcFinalDeathFactTracker))],
                Readers = [Code(NpcReactionsCode), Code(BossSpawnPointCode)]
            },
            new()
            {
                Template = Family(FactKeys.BossRespawnDeathsHead, BossParameter),
                Writers = [Code(nameof(BossRespawnTracker))],
                Readers = [Code(nameof(BossRespawnTracker))]
            },
            new()
            {
                Template = Family(FactKeys.NpcTalkedHead, NpcParameter),
                Writers = [Code(nameof(DialogueService))]
            },
            new()
            {
                Template = Family(FactKeys.DialogueOptionUsedHead, NpcParameter, NodeParameter, OptionParameter),
                Writers = [Code(nameof(DialogueService))],
                Readers = [Code(nameof(DialogueService))]
            },
            new()
            {
                Template = Family(FactKeys.SpeechCheckRewardedHead, NpcParameter, NodeParameter, OptionParameter),
                Writers = [Code(nameof(DialogueService))],
                Readers = [Code(nameof(DialogueService))]
            },
            new()
            {
                Template = Family(FactKeys.QuestOfferRollUntilHead, QuestParameter),
                Writers = [Code(nameof(QuestOfferRollCondition))],
                Readers = [Code(nameof(QuestOfferRollCondition))]
            },
            new()
            {
                Template = Family(FactKeys.QuestOfferRollPassedHead, QuestParameter),
                Writers = [Code(nameof(QuestOfferRollCondition))],
                Readers = [Code(nameof(QuestOfferRollCondition))]
            },
            new()
            {
                Template = Family(FactKeys.ItemEquippedHead, PieceParameter),
                Writers = [Code(nameof(EquipFactTracker))]
            },
            new()
            {
                Template = Family(FactKeys.ItemEquippedAnyKey),
                Writers = [Code(nameof(EquipFactTracker))]
            },
        ];

        /// <summary>One address in the code, told apart from every address in the data.</summary>
        public static string Code(string name) => CodePrefix + name;

        /// <summary>Whether a written key still carries a parameter of the family it is spelled after — a
        /// template taken out of a list and written down as it stood. The game raises the KEYS of a family
        /// and never the spelling of one, so such a word is a key nothing will ever answer.</summary>
        public static bool Unfilled(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return key.Contains(ParameterOpen) || key.Contains(ParameterClose);
        }

        /// <summary>The template of one family: the head its builder writes, and its parameters named the
        /// way the builder names them. A test calls every builder and holds what comes out against these,
        /// so a family renamed is not renamed on one side only.</summary>
        private static string Family(string head, params string[] parameters) =>
            string.Join(
                FactKeys.Separator,
                parameters.Select(parameter => $"{ParameterOpen}{parameter}{ParameterClose}").Prepend(head));
    }

    /// <summary>One key of a reading of the game: the word itself, the family it belongs to, and every
    /// place that writes it or reads it back.</summary>
    public sealed record FactKeyEntry
    {
        /// <summary>The key as it is written, or the template of the family for a row that IS one.</summary>
        public required string Key { get; init; }

        /// <summary>The template of the declared family this key belongs to; null for a key no code
        /// declares — a word an author invented, which is legal and merely unanswered by the code.</summary>
        public string? Family { get; init; }

        /// <summary>The row is a declared family rather than a word read out of a document.</summary>
        public bool Declared { get; init; }

        public required IReadOnlyList<string> Writers { get; init; }

        public required IReadOnlyList<string> Readers { get; init; }

        /// <summary>Somebody asks about the key and nothing ever raises it: the clause gated on it can
        /// never be met.</summary>
        public bool NeverWritten => Writers.Count == 0 && Readers.Count > 0;

        /// <summary>The key is raised and nobody ever asks: the write is a step nothing hangs on.</summary>
        public bool NeverRead => Readers.Count == 0 && Writers.Count > 0;
    }

    /// <summary>
    /// Every fact key of one reading of the game, and who writes and reads each one: the families the code
    /// keeps, and the words the documents themselves write. One list, because "is this key answered" is
    /// the same question whether the answer lives in a quest or in a tracker nobody wrote a document for.
    /// <para>A report and not a gate: a key nothing declares is an author's own word, offered and never
    /// refused. What the registry says about it is only who writes it and who reads it back.</para>
    /// </summary>
    public sealed class FactKeyRegistry
    {
        /// <summary>The declared families first, in the order the code declares them, and then the words
        /// the documents write, in the order they were met. Fixed, so a reading can be pinned.</summary>
        public IReadOnlyList<FactKeyEntry> Keys { get; }

        private FactKeyRegistry(IReadOnlyList<FactKeyEntry> keys) => Keys = keys;

        /// <summary>Builds the registry over the uses one walk of the documents found. Blank keys are
        /// passed over, and so are the templates of a family written as they stand: both are a key left
        /// half-typed, which the walk itself names where it is written. Taking a template in would fold it
        /// into the very family it is spelled after, and the word nothing raises would read as raised.</summary>
        public static FactKeyRegistry Over(IEnumerable<FactKeyUse> uses)
        {
            ArgumentNullException.ThrowIfNull(uses);

            List<FactKeyUse> written =
                [.. uses.Where(use => use.Key is { Length: > 0 } key && !FactKeyDeclarations.Unfilled(key))];

            List<FactKeyEntry> keys = [.. FactKeyDeclarations.All.Select(declaration => Declared(declaration, written))];
            HashSet<string> named = [.. keys.Select(key => key.Key)];

            keys.AddRange(Found(written).Where(key => !named.Contains(key)).Select(key => Wrote(key, written)));

            return new FactKeyRegistry(keys);
        }

        /// <summary>One declared family: the code that keeps it, and every document address writing or
        /// reading any key of it. The family answers for its members, so a counter written per npc is not
        /// reported unwritten because no tracker names that npc.</summary>
        private static FactKeyEntry Declared(FactKeyDeclaration declaration, IReadOnlyList<FactKeyUse> uses)
        {
            IReadOnlyList<FactKeyUse> mine = [.. uses.Where(use => declaration.Covers(use.Key))];

            return new FactKeyEntry
            {
                Key = declaration.Template,
                Family = declaration.Template,
                Declared = true,
                Writers = [.. declaration.Writers, .. Addresses(mine, FactKeyUseKind.Write)],
                Readers = [.. declaration.Readers, .. Addresses(mine, FactKeyUseKind.Read)]
            };
        }

        /// <summary>One word a document writes: its own addresses, and the code of the family it belongs
        /// to — the tracker keeping <c>Kill_Count</c> writes the key a quest counts, whether or not any
        /// code names that npc.</summary>
        private static FactKeyEntry Wrote(string key, IReadOnlyList<FactKeyUse> uses)
        {
            FactKeyDeclaration? family = FactKeyDeclarations.All.FirstOrDefault(declaration => declaration.Covers(key));
            IReadOnlyList<FactKeyUse> mine = [.. uses.Where(use => string.Equals(use.Key, key, StringComparison.Ordinal))];

            return new FactKeyEntry
            {
                Key = key,
                Family = family?.Template,
                Writers = [.. family?.Writers ?? [], .. Addresses(mine, FactKeyUseKind.Write)],
                Readers = [.. family?.Readers ?? [], .. Addresses(mine, FactKeyUseKind.Read)]
            };
        }

        /// <summary>The distinct words the documents write, in the order they were met.</summary>
        private static IEnumerable<string> Found(IEnumerable<FactKeyUse> uses) =>
            uses.Select(use => use.Key).Distinct(StringComparer.Ordinal);

        private static IEnumerable<string> Addresses(IEnumerable<FactKeyUse> uses, FactKeyUseKind kind) =>
            uses.Where(use => use.Kind == kind).Select(use => use.Where);
    }
}
