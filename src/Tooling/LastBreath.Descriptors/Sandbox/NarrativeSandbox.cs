namespace LastBreath.Descriptors.Sandbox
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Dialogues;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Save;
    using Core.Services;
    using Newtonsoft.Json;
    using Tooling.Catalogs;
    using static Tooling.Text.Format;

    /// <summary>
    /// The game's narrative running over a world nobody is playing: the real dialogue service, the real
    /// quest log, the real condition and action parsers, standing on facts, a bag and a standing typed
    /// by an author instead of earned by a player.
    /// <para>The documents are the ones open in the tool, so a dialogue reads the way it is written at
    /// this moment and not the way it was last saved. Everything a running game would do that a tool
    /// cannot is named in the run's log rather than performed.</para>
    /// </summary>
    public sealed class NarrativeSandbox
    {
        private const string ReadFailedFormat = "{0} could not be read: {1}";

        private const string NoCatalogFormat = "the {0} catalog is not among the ones this run opened";

        private const string NoCurvesFormat =
            "'{0}' holds no file: the speech-check and quest-offer chances fall back to their built-in defaults";

        private const string NoPlayerNote =
            "the Attribute condition is never met here: a sandbox holds no character sheet to ask";

        private const string SeededQuestNote =
            "a seeded quest stands on the first stage of its record and its journal is not re-evaluated: "
            + "stages are entered and left when a choice moves the world, not when the world is typed";

        private readonly WorldFactsService _facts = new();
        private readonly SandboxBag _bag = new();
        private readonly SandboxInfluence _influence;
        private readonly QuestLogService _quests;
        private readonly IDialogueProvider _dialogues;
        private readonly IDialogueService _dialogue;
        private readonly List<string> _notes = [];

        /// <summary>The world its author typed. Written into the services by <see cref="Restore"/>;
        /// a run mutates the services and leaves this alone, which is what a reset goes back to.</summary>
        public SandboxWorldState State { get; }

        public SandboxLog Log { get; } = new();

        /// <summary>What the run could not read or cannot answer for: a catalog missing, a document that
        /// is not a dialogue, a vocabulary that has drifted from the game's, and the two things a
        /// sandbox is silent about by nature — the player's attributes and a quest's own journal.</summary>
        public IReadOnlyList<string> Notes => _notes;

        public IDialogueProvider Dialogues => _dialogues;

        public IDialogueService Dialogue => _dialogue;

        /// <summary>The facts as the run has left them.</summary>
        public IReadOnlyDictionary<string, int> Facts => _facts.Snapshot;

        /// <summary>The bag as the run has left it.</summary>
        public IReadOnlyDictionary<string, int> Items => _bag.Amounts;

        /// <summary>The quests as the run has left them, each on the stage it stands on.</summary>
        public IReadOnlyList<QuestState> Quests => [.. _quests.States];

        /// <summary>The rolls of the run. Pure C# rather than the engine's: a tool is not a game, and
        /// which way a roll comes out is the author's switch to throw (<see cref="SandboxRolls"/>),
        /// not a seed for him to guess.</summary>
        private IRandomNumberGenerator Rnd { get; } = new DefaultRandomNumberGenerator();

        private NarrativeSandbox(CatalogWorkspace workspace, SandboxWorldState state)
        {
            State = state;

            var world = new SandboxWorld();
            var standing = new SandboxStanding(state);
            var messages = new SandboxMessages(Log);
            var events = new GameEventBus();
            var minter = new SandboxItemMinter();

            _influence = new SandboxInfluence(state, Influence(workspace.Root, messages));

            // The two parsers, the two providers and the quest log stand in a ring — the game breaks it
            // the same way, by handing the factories a way to ask for the log rather than the log itself.
            QuestProvider? quests = null;
            QuestLogService? log = null;

            var conditionFactories = NarrativeFactories.Conditions(
                _bag, _facts, standing, standing, world, _influence, world, Rnd, () => log!, () => quests!);

            var actionFactories = SandboxVocabulary.Watched(Log, NarrativeFactories.Actions(
                _facts, _bag, minter, events, world, standing, _influence, world, messages,
                world, world, world, world, world, () => log!));

            _notes.AddRange(SandboxVocabulary.Notes(conditionFactories, actionFactories));
            _notes.Add(NoPlayerNote);
            _notes.Add(SeededQuestNote);

            var conditions = new NarrativeConditionParser(conditionFactories);
            var actions = new NarrativeActionParser(actionFactories);

            quests = new QuestProvider(conditions, actions);
            var dialogues = new DialogueProvider(conditions, actions);

            log = new QuestLogService(quests, _facts, _bag, minter, new SandboxUniqueItems(),
                _influence, world, world, events, messages, new LoadScope());

            _quests = log;
            _dialogues = dialogues;
            _dialogue = new DialogueService(dialogues, _facts, _influence, Rnd, events);

            Read(workspace, DataCatalog.Quests, quests);
            Read(workspace, DataCatalog.Dialogues, dialogues);
        }

        /// <summary>Opens a sandbox over the documents this tool has open. Read afresh on every run: a
        /// dialogue edited a keystroke ago is the dialogue the next run reads.</summary>
        public static NarrativeSandbox Load(CatalogWorkspace workspace, SandboxWorldState state)
        {
            ArgumentNullException.ThrowIfNull(workspace);
            ArgumentNullException.ThrowIfNull(state);

            return new NarrativeSandbox(workspace, state);
        }

        /// <summary>What the check is worth at the influence level the author typed — the number shown
        /// beside the option, whatever the switch does to the roll itself.</summary>
        public float SpeechCheckChance(int difficulty) => _influence.TrueSpeechCheckChance(difficulty);

        /// <summary>Puts the authored world back into the services and ends whatever conversation was
        /// running. Silent on purpose: a reset is not a pick-up, a kill or a discovery, and waking the
        /// quest log with each restored fact would run stage entries and exits nobody walked into.</summary>
        public void Restore()
        {
            _dialogue.End();
            _facts.RestoreState(State.Facts);
            _bag.Restore(State.Items);
            _influence.RestoreState(State.InfluenceLevel, 0);
            _quests.RestoreState(State.Quests.Select(entry => new QuestState(entry.Key) { Status = entry.Value }));
        }

        /// <summary>The game's own Influence mastery over the shipped curve. Read off disk rather than
        /// out of the workspace: no schema describes that catalog, and the numbers behind a chance are
        /// not what this tool edits. A root without it is a note — the built-in defaults are a curve
        /// nobody authored, and a chance shown beside an option has to be one somebody did.</summary>
        private InfluenceMastery Influence(string root, SandboxMessages messages)
        {
            var mastery = new InfluenceMastery(messages);
            string folder = CatalogWorkspace.Folder(root, DataCatalog.Influence);
            IReadOnlyList<string> paths = CatalogWorkspace.FilePaths(folder);

            if (paths.Count == 0) _notes.Add(Text(NoCurvesFormat, folder));

            foreach (string path in paths)
                Guarded(Path.GetFileName(path), () => mastery.Apply(DataCatalog.Influence, new GameDataFile(Path.GetFileName(path), File.ReadAllText(path))));

            return mastery;
        }

        /// <summary>Hands one catalog's open documents to the game's own provider. A catalog this run
        /// never opened is a note and not an empty provider passed off as a read one.</summary>
        private void Read(CatalogWorkspace workspace, string catalog, IGameDataParticipant participant)
        {
            CatalogView? view = workspace.Catalogs.FirstOrDefault(entry => string.Equals(entry.Catalog, catalog, StringComparison.Ordinal));
            if (view is null)
            {
                _notes.Add(Text(NoCatalogFormat, catalog));
                return;
            }

            foreach (CatalogFile file in view.Files)
                Guarded(file.Name, () => participant.Apply(catalog, new GameDataFile(file.Name, file.Document.Root.ToString(Formatting.None))));
        }

        /// <summary>One document read, or one note saying why it was not. The providers already drop a
        /// broken record on their own; this catches the file that is not the shape they expect at all.</summary>
        private void Guarded(string name, Action read)
        {
            try
            {
                read();
            }
            catch (Exception failure) when (failure is IOException or JsonException or InvalidOperationException or ArgumentException)
            {
                _notes.Add(Text(ReadFailedFormat, name, failure.Message));
            }
        }
    }
}
