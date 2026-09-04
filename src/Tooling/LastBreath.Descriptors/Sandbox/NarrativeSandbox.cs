namespace LastBreath.Descriptors.Sandbox
{
    using System;
    using System.Collections.Generic;
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
        private const string NoCurvesFormat =
            "'{0}' holds no file: the speech-check and quest-offer chances fall back to their built-in defaults";

        private const string NoPlayerNote =
            "the Attribute condition is never met here: a sandbox holds no character sheet to ask";

        /// <summary>What a world holding a quest is silent about. Public because a reader holding the run
        /// has to be able to tell this caveat from the rest without matching on its wording.</summary>
        public const string SeededQuestNote =
            "a seeded quest stands on the first stage of its record and its journal is not re-evaluated: "
            + "stages are entered and left when a choice moves the world, not when the world is typed";

        private readonly WorldFactsService _facts = new();
        private readonly SandboxBag _bag = new();
        private readonly SandboxInfluence _influence;
        private readonly QuestLogService _quests;
        private readonly IDialogueProvider _dialogues;
        private readonly IDialogueService _dialogue;
        private readonly List<string> _notes = [];
        private readonly List<string> _caveats = [];

        /// <summary>The world its author typed. Written into the services by <see cref="Restore"/>;
        /// a run mutates the services and leaves this alone, which is what a reset goes back to.</summary>
        public SandboxWorldState State { get; }

        public SandboxLog Log { get; } = new();

        /// <summary>What the run could not read: a catalog missing, a document that is not a dialogue, a
        /// vocabulary that has drifted from the game's. Every one of them is about THIS reading of the
        /// documents, which is why a report of the checks carries them and nothing else.</summary>
        public IReadOnlyList<string> Notes => _notes;

        /// <summary>What a sandbox is silent about by nature — the player's attributes, a seeded quest's
        /// own journal. Held apart from the notes because they are true of every run whatever the
        /// documents say: a reader walking a conversation needs them, and a report of what the data owes
        /// would only be padded by them.</summary>
        public IReadOnlyList<string> Caveats => _caveats;

        public IDialogueProvider Dialogues => _dialogues;

        /// <summary>The quests as the CATALOG was read, which is not the same question as where they
        /// stand: what a document writes and this does not hold was dropped by the loader.</summary>
        public IQuestProvider QuestCatalog { get; }

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
            _caveats.Add(NoPlayerNote);

            // Only where a quest was actually seeded: the stage a run starts one on is a fact about the
            // world its author typed, and a run with no quests in it has nothing to be silent about.
            if (State.Quests.Count > 0) _caveats.Add(SeededQuestNote);

            var conditions = new NarrativeConditionParser(conditionFactories);
            var actions = new NarrativeActionParser(actionFactories);

            quests = new QuestProvider(conditions, actions);
            var dialogues = new DialogueProvider(conditions, actions);

            log = new QuestLogService(quests, _facts, _bag, minter, new SandboxUniqueItems(),
                _influence, world, world, events, messages, new LoadScope());

            _quests = log;
            _dialogues = dialogues;
            QuestCatalog = quests;
            _dialogue = new DialogueService(dialogues, _facts, _influence, Rnd, events);

            NarrativeDocuments.Read(workspace, DataCatalog.Quests, quests, _notes);
            NarrativeDocuments.Read(workspace, DataCatalog.Dialogues, dialogues, _notes);
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

            if (CatalogWorkspace.FilePaths(folder).Count == 0) _notes.Add(Text(NoCurvesFormat, folder));

            NarrativeDocuments.Read(folder, DataCatalog.Influence, mastery, _notes);

            return mastery;
        }
    }
}
