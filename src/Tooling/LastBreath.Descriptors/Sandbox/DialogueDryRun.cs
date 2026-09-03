namespace LastBreath.Descriptors.Sandbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Narrative.Dialogues;
    using static Tooling.Text.Format;

    /// <summary>
    /// One option of the node a run is standing on, as the author needs to read it: whether the game
    /// would show it at all, whether it would let him press it, what a check on it is worth, and where
    /// it leads. A hidden option is listed rather than dropped — the question an author brings to a dry
    /// run is usually about the line he cannot see.
    /// </summary>
    public sealed record DryRunOption(
        string Id, string TextKey, bool Visible, bool Enabled, float? SpeechCheckChance, string? NextNodeId);

    /// <summary>Where a run stands: the node, its lines by key, its options, and everything done to the
    /// sandbox to get here. <see cref="Note"/> carries why a run is not running.</summary>
    public sealed record DryRunState(
        string NpcId,
        string? NodeId,
        IReadOnlyList<DialogueLine> Lines,
        IReadOnlyList<DryRunOption> Options,
        bool Running,
        string Note,
        IReadOnlyList<string> Log)
    {
        public static DryRunState Idle { get; } = new(string.Empty, null, [], [], false, string.Empty, []);
    }

    /// <summary>
    /// A conversation read without playing it: the game's own dialogue service walked one option at a
    /// time over a world its author typed. Choosing runs the real actions against the sandbox, so the
    /// facts, the bag and the quests move exactly as far as the conversation moves them.
    /// </summary>
    public sealed class DialogueDryRun(NarrativeSandbox sandbox)
    {
        /// <summary>How many choices one run makes before it stops itself. A dialogue may loop by
        /// design — every "back" option does — so a run that never ends is not a broken document, and
        /// stopping is how a tool says the loop closed rather than hanging inside it.</summary>
        public const int StepLimit = 200;

        private const string NoDialogueFormat = "'{0}' has no readable dialogue in the documents this run opened";

        private const string NoEntryFormat = "no entry rule of '{0}' is met by this world: the game would not open the conversation";

        private const string EndedText = "the conversation ended";

        private const string LimitFormat = "the run stopped after {0} choices: the conversation is going in circles";

        private const string StoppedFormat = "the run stopped: {0}";

        private const string ChoseFormat = "chose '{0}'";

        private DialogueDefinition? _dialogue;
        private string _npcId = string.Empty;
        private int _steps;

        public DryRunState State { get; private set; } = DryRunState.Idle;

        /// <summary>Puts the authored world back and opens the conversation of one npc.</summary>
        public void Start(string npcId)
        {
            _npcId = npcId;
            _steps = 0;
            sandbox.Log.Clear();
            sandbox.Restore();

            _dialogue = sandbox.Dialogues.Get(npcId);
            if (_dialogue is null)
            {
                State = Stopped(Text(NoDialogueFormat, npcId));
                return;
            }

            bool opened = false;
            if (!Guarded(() => opened = sandbox.Dialogue.Start(npcId, sandbox.State.NpcInstanceId, sandbox.State.NpcFaction))) return;

            if (!opened)
            {
                State = Stopped(Text(NoEntryFormat, npcId));
                return;
            }

            Show();
        }

        /// <summary>Presses one option: its actions run against the sandbox and the run moves to
        /// wherever the game takes it. An option the game would not show or would not enable is not
        /// pressable here either.</summary>
        public void Choose(string optionId)
        {
            if (!State.Running) return;

            if (_steps >= StepLimit)
            {
                State = Stopped(Text(LimitFormat, StepLimit));
                return;
            }

            if (State.Options.FirstOrDefault(option => option.Id == optionId) is not { Visible: true, Enabled: true }) return;

            _steps++;
            sandbox.Log.Write(Text(ChoseFormat, optionId));

            if (!Guarded(() => sandbox.Dialogue.Choose(optionId))) return;

            Show();
        }

        /// <summary>Starts the same conversation over on the authored world, forgetting everything the
        /// run wrote into the sandbox.</summary>
        public void Reset()
        {
            if (_npcId.Length > 0) Start(_npcId);
        }

        /// <summary>
        /// The node the service is standing on, with every option of it — not only the ones it handed
        /// back. Visibility and enablement are read off the service's own answer rather than worked out
        /// again here: two readings of "would the game show this" would be two answers.
        /// </summary>
        private void Show()
        {
            if (sandbox.Dialogue.Current is not { } view)
            {
                State = Stopped(EndedText);
                return;
            }

            DialogueNode? node = NodeOf(view);
            IReadOnlyList<DryRunOption> options = node is null
                ? [.. view.Options.Select(option => new DryRunOption(option.Id, option.TextKey, true, option.Enabled, null, null))]
                : [.. node.Options.Select(option => Read(option, view))];

            State = new DryRunState(_npcId, node?.Id, view.Lines, options, true, string.Empty, [.. sandbox.Log.Lines]);
        }

        /// <summary>Which node the service opened. Answered by the very list of lines it handed back:
        /// each node owns its own, so the one that is that list is the one being shown.</summary>
        private DialogueNode? NodeOf(DialogueNodeView view) =>
            _dialogue?.Nodes.Values.FirstOrDefault(node => ReferenceEquals(node.Lines, view.Lines));

        private DryRunOption Read(DialogueOption option, DialogueNodeView view)
        {
            DialogueOptionView? shown = view.Options.FirstOrDefault(entry => entry.Id == option.Id);

            return new DryRunOption(
                option.Id,
                option.TextKey,
                shown != null,
                shown?.Enabled ?? false,
                option.SpeechCheck is { } check ? sandbox.SpeechCheckChance(check.Difficulty) : null,
                option.NextNodeId);
        }

        /// <summary>One step of the conversation, or the run stopped with the reason on it. A dialogue
        /// is data being edited: an entry that throws is what the author came here to find, and a tool
        /// that died on it would show him nothing.</summary>
        private bool Guarded(Action step)
        {
            try
            {
                step();
                return true;
            }
            catch (Exception failure)
            {
                State = Stopped(Text(StoppedFormat, failure.Message));
                return false;
            }
        }

        private DryRunState Stopped(string note)
        {
            sandbox.Dialogue.End();

            return new DryRunState(_npcId, null, [], [], false, note, [.. sandbox.Log.Lines]);
        }
    }
}
