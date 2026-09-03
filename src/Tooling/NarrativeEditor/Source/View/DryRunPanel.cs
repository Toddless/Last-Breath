namespace NarrativeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Narrative.Dialogues;
    using Core.Narrative.Quests;
    using Godot;
    using LastBreath.Descriptors.Sandbox;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Localization;
    using static Tooling.Text.Format;

    /// <summary>
    /// Reading a dialogue by walking it. The author states a world — what has happened, what is in the
    /// bag, where the quests stand, how well the player talks and what the person across the table
    /// thinks of him — and the panel opens the conversation of the record on screen with the game's own
    /// dialogue service, from the document as it is written this second.
    /// <para>Every option of the node is listed, the ones the game would hide among them, with what
    /// hides it and what a check on it is worth. Pressing one runs its actions against the sandbox: the
    /// facts, the bag and the quests underneath move, and the panel shows them moving.</para>
    /// </summary>
    public partial class DryRunPanel : VBoxContainer
    {
        private const string Title = "dry run";

        private const string RunText = "Run";
        private const string ResetText = "Reset";

        private const string FactsTitle = "facts";
        private const string ItemsTitle = "bag";
        private const string QuestsTitle = "quests";

        private const string KeyHint = "key";
        private const string ItemHint = "item id";
        private const string QuestHint = "quest id";
        private const string AddText = "+";

        private const string InfluenceTitle = "influence level";
        private const string FactionTitle = "npc faction";
        private const string StandingTitle = "faction standing";
        private const string RelationTitle = "npc relation";
        private const string RollsTitle = "rolls";

        private const string NodeFormat = "node: {0}";
        private const string LineFormat = "{0}: {1}";
        private const string OptionFormat = "{0}{1}   {2}";
        private const string TranslatedFormat = "{0}   —   {1}";
        private const string CountFormat = "{0} = {1}";
        private const string QuestRowFormat = "{0} = {1}";

        /// <summary>A quest of the run is read with the stage it stands on: a seeded one stands on the
        /// first stage of its record, and a reader who cannot see which one is reading half an answer.</summary>
        private const string QuestStageFormat = "{0} = {1} on '{2}'";

        private const string HiddenMark = "[hidden]  ";
        private const string DisabledMark = "[disabled]  ";
        private const string CheckFormat = "[speech check {0}%]  ";

        private const string NoRecordText = "open a dialogue to run it";
        private const string StaleText = "the document changed: press Run again";
        private const string NotADialogueText = "the record on screen is not a dialogue";

        private const string StateNowTitle = "world after the run";
        private const string LogTitle = "run log";

        private const string RanFormat = "dry run of '{0}'";

        /// <summary>What one line of the readouts ends with.</summary>
        private const string LineBreak = "\n";

        private const string Separator = ", ";

        /// <summary>How tall the pane asks to be before the divider above it is dragged.</summary>
        private const int PaneHeight = 260;

        private const int KeyWidth = 220;

        private const int MaxCount = 9999;

        private const float Percent = 100f;

        private readonly SandboxWorldState _state = new();

        private LineEdit _factKey = null!;
        private SpinBox _factCount = null!;
        private DryRunRows _factRows = null!;

        private LineEdit _itemId = null!;
        private SpinBox _itemCount = null!;
        private DryRunRows _itemRows = null!;

        private LineEdit _questId = null!;
        private OptionButton _questStatus = null!;
        private DryRunRows _questRows = null!;

        private SpinBox _influence = null!;
        private OptionButton _faction = null!;
        private OptionButton _standing = null!;
        private OptionButton _relation = null!;
        private OptionButton _rolls = null!;

        private Button _run = null!;
        private Button _reset = null!;

        private Label _node = null!;
        private VBoxContainer _lines = null!;
        private VBoxContainer _options = null!;
        private Label _note = null!;
        private Label _now = null!;
        private Label _log = null!;

        private CatalogFile? _file;
        private CatalogRecord? _record;
        private bool _isDialogue;

        private NarrativeSandbox? _sandbox;
        private DialogueDryRun? _dry;

        /// <summary>A rebuild is already waiting for the end of the frame. Every gesture of the shell
        /// asks for one — a letter typed into the inspector redraws the whole tool — and a panel rebuilt
        /// per letter is one nobody can press a button in.</summary>
        private bool _pending;

        /// <summary>The world was typed into since the rows were last drawn. Nothing else changes them,
        /// so a redraw asked for by the rest of the tool leaves them standing.</summary>
        private bool _typed = true;

        /// <summary>The step of the run whose lines and options are on screen.</summary>
        private DryRunState? _shown;

        /// <summary>The .po files of the run, for showing what a line key says. Null falls back to the
        /// key itself, which is what the outline does.</summary>
        public LocalizedTexts? Texts { get; set; }

        /// <summary>Everything the tool has open — the sandbox reads the narrative catalogs out of it
        /// afresh on every run.</summary>
        public CatalogWorkspace? Workspace { get; set; }

        /// <summary>What the run came to, for the status line of the shell.</summary>
        public event Action<string>? Said;

        public override void _Ready()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            CustomMinimumSize = new Vector2(0, PaneHeight);

            AddChild(Heading(Title));
            AddChild(Buttons());
            AddChild(new HSeparator());
            AddChild(Facts());
            AddChild(Items());
            AddChild(Quests());
            AddChild(Dials());
            AddChild(new HSeparator());

            _node = new Label();
            _lines = new VBoxContainer();
            _options = new VBoxContainer();
            _note = Wrapped();
            _now = Wrapped();
            _log = Wrapped();

            AddChild(_node);
            AddChild(_lines);
            AddChild(_options);
            AddChild(_note);
            AddChild(Heading(StateNowTitle));
            AddChild(_now);
            AddChild(Heading(LogTitle));
            AddChild(_log);

            Redraw();
        }

        /// <summary>Which record the tool is standing on. A different file, or an edit inside the one
        /// the run was started from, drops what is on screen: a walk of a document that has since been
        /// rewritten is a reading of something that no longer exists.</summary>
        public void Standing(CatalogRecord? record, bool isDialogue)
        {
            bool moved = !Same(_record, record);

            _record = record;
            _isDialogue = isDialogue;

            if (!ReferenceEquals(_file, record?.File))
            {
                if (_file is { } held) held.Document.Changed -= Stale;

                _file = record?.File;
                _file?.Document.Changed += Stale;
            }

            if (moved) Invalidate();
            else Redraw();
        }

        public override void _ExitTree()
        {
            if (_file is { } held) held.Document.Changed -= Stale;
        }

        /// <summary>Whether two rows are the same place of the same file — nothing on both sides
        /// included, which is where the panel stands while no record is open.</summary>
        private static bool Same(CatalogRecord? one, CatalogRecord? other)
        {
            if (one is null || other is null) return one is null && other is null;

            return ReferenceEquals(one.File, other.File) && one.Pointer == other.Pointer;
        }

        private static Label Heading(string text) => new() { Text = text };

        private static Label Wrapped() => new()
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        private static SpinBox Counter(int value) => new()
        {
            MinValue = 0,
            MaxValue = MaxCount,
            Step = 1,
            Value = value
        };

        /// <summary>An option button over every member of an enum, in the enum's own order.</summary>
        private static OptionButton Choices<T>(T selected) where T : struct, Enum
        {
            var button = new OptionButton();
            T[] values = Members<T>();

            foreach (T value in values)
                button.AddItem(value.ToString());

            button.Select(Array.IndexOf(values, selected));

            return button;
        }

        private static T Chosen<T>(OptionButton button) where T : struct, Enum => Members<T>()[button.Selected];

        /// <summary>The members of an enum, read once and handed out: the option buttons are drawn from
        /// the same array their selection is read back through.</summary>
        private static T[] Members<T>() where T : struct, Enum => EnumMembers<T>.All;

        private HBoxContainer Buttons()
        {
            _run = new Button { Text = RunText };
            _reset = new Button { Text = ResetText };

            _run.Pressed += Start;
            _reset.Pressed += Again;

            var row = new HBoxContainer();
            row.AddChild(_run);
            row.AddChild(_reset);

            return row;
        }

        private VBoxContainer Facts()
        {
            _factKey = new LineEdit { PlaceholderText = KeyHint, CustomMinimumSize = new Vector2(KeyWidth, 0) };
            _factCount = Counter(1);
            _factRows = new DryRunRows();
            _factRows.Removed += key =>
            {
                _state.Facts.Remove(key);
                _typed = true;
                Redraw();
            };

            var add = new Button { Text = AddText };
            add.Pressed += () => Add(_state.Facts, _factKey, (int)_factCount.Value);

            return Section(FactsTitle, [_factKey, _factCount, add], _factRows);
        }

        private VBoxContainer Items()
        {
            _itemId = new LineEdit { PlaceholderText = ItemHint, CustomMinimumSize = new Vector2(KeyWidth, 0) };
            _itemCount = Counter(1);
            _itemRows = new DryRunRows();
            _itemRows.Removed += key =>
            {
                _state.Items.Remove(key);
                _typed = true;
                Redraw();
            };

            var add = new Button { Text = AddText };
            add.Pressed += () => Add(_state.Items, _itemId, (int)_itemCount.Value);

            return Section(ItemsTitle, [_itemId, _itemCount, add], _itemRows);
        }

        private VBoxContainer Quests()
        {
            _questId = new LineEdit { PlaceholderText = QuestHint, CustomMinimumSize = new Vector2(KeyWidth, 0) };
            _questStatus = Choices(QuestStatus.Active);
            _questRows = new DryRunRows();
            _questRows.Removed += key =>
            {
                _state.Quests.Remove(key);
                _typed = true;
                Redraw();
            };

            var add = new Button { Text = AddText };
            add.Pressed += () =>
            {
                if (_questId.Text.Length == 0) return;

                _state.Quests[_questId.Text] = Chosen<QuestStatus>(_questStatus);
                _questId.Text = string.Empty;
                _typed = true;
                Redraw();
            };

            return Section(QuestsTitle, [_questId, _questStatus, add], _questRows);
        }

        /// <summary>The single-valued half of the world: everything an author sets rather than lists.</summary>
        private HBoxContainer Dials()
        {
            _influence = Counter(_state.InfluenceLevel);
            _influence.ValueChanged += value => _state.InfluenceLevel = (int)value;

            _faction = Choices(_state.NpcFaction);
            _faction.ItemSelected += _ => _state.NpcFaction = Chosen<Fractions>(_faction);

            _standing = Choices(_state.FactionStanding);
            _standing.ItemSelected += _ => _state.FactionStanding = Chosen<RelationLevel>(_standing);

            _relation = Choices(_state.NpcRelation);
            _relation.ItemSelected += _ => _state.NpcRelation = Chosen<RelationLevel>(_relation);

            _rolls = Choices(_state.Rolls);
            _rolls.ItemSelected += _ => _state.Rolls = Chosen<SandboxRolls>(_rolls);

            var row = new HBoxContainer();

            foreach ((string title, Control control) in
                     new (string, Control)[]
                     {
                         (InfluenceTitle, _influence), (FactionTitle, _faction), (StandingTitle, _standing),
                         (RelationTitle, _relation), (RollsTitle, _rolls)
                     })
            {
                row.AddChild(Heading(title));
                row.AddChild(control);
            }

            return row;
        }

        private static VBoxContainer Section(string title, IReadOnlyList<Control> header, DryRunRows rows)
        {
            var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var line = new HBoxContainer();

            line.AddChild(Heading(title));
            foreach (Control control in header) line.AddChild(control);

            box.AddChild(line);
            box.AddChild(rows);

            return box;
        }

        private void Add(Dictionary<string, int> into, LineEdit key, int count)
        {
            if (key.Text.Length == 0 || count <= 0) return;

            into[key.Text] = count;
            key.Text = string.Empty;
            _typed = true;
            Redraw();
        }

        /// <summary>Opens the conversation of the record on screen over the world as it is typed now.</summary>
        private void Start()
        {
            if (Workspace is not { } workspace || _record is not { } record || !_isDialogue) return;

            _sandbox = NarrativeSandbox.Load(workspace, _state);
            _dry = new DialogueDryRun(_sandbox);
            _dry.Start(record.CurrentId);

            Said?.Invoke(Text(RanFormat, record.CurrentId));
            Redraw();
        }

        private void Again()
        {
            _dry?.Reset();
            Redraw();
        }

        private void Choose(string optionId)
        {
            _dry?.Choose(optionId);
            Redraw();
        }

        /// <summary>A run reading a document that has since been edited says so instead of showing a
        /// walk of a dialogue nobody has any more.</summary>
        private void Stale(JsonPointer pointer) => Invalidate();

        private void Invalidate()
        {
            _dry = null;
            _sandbox = null;
            Redraw();
        }

        /// <summary>Asks for a rebuild at the end of the frame. Deferred for the reason the inspector
        /// defers its own: a row can go while the author is taking it out from inside the panel, and
        /// tearing the button down from under the press is what that costs.</summary>
        private void Redraw()
        {
            if (_run is null || _pending) return;

            _pending = true;
            Callable.From(Rebuild).CallDeferred();
        }

        private void Rebuild()
        {
            _pending = false;

            if (_run is null || !IsInsideTree()) return;

            _run.Disabled = _record is null || !_isDialogue || Workspace is null;
            _reset.Disabled = _dry is null;

            ShowRows();
            ShowRun();
        }

        /// <summary>The world as its author typed it. Drawn only after he has typed: these rows are his
        /// own input, and rebuilding them for somebody else's keystroke is how input gets lost.</summary>
        private void ShowRows()
        {
            if (!_typed) return;

            _typed = false;

            _factRows.Show([.. _state.Facts.Select(entry => (entry.Key, Text(CountFormat, entry.Key, entry.Value)))]);
            _itemRows.Show([.. _state.Items.Select(entry => (entry.Key, Text(CountFormat, entry.Key, entry.Value)))]);
            _questRows.Show([.. _state.Quests.Select(entry => (entry.Key, Text(QuestRowFormat, entry.Key, entry.Value)))]);
        }

        /// <summary>The step of the conversation on screen. Redrawn when the run has moved and not
        /// before: every step is a fresh state, so the one being looked at is the one already drawn.</summary>
        private void ShowRun()
        {
            DryRunState? step = _dry?.State;

            if (ReferenceEquals(step, _shown) && (step != null || _note.Text == Idle())) return;

            _shown = step;

            DryRunRows.Clear(_lines);
            DryRunRows.Clear(_options);

            if (step is not { } run || _sandbox is not { } sandbox)
            {
                _node.Text = string.Empty;
                _note.Text = Idle();
                _now.Text = string.Empty;
                _log.Text = string.Empty;
                return;
            }

            _node.Text = Text(NodeFormat, run.NodeId ?? string.Empty);
            _note.Text = string.Join(LineBreak, [run.Note, .. sandbox.Notes]).Trim();
            _now.Text = World(sandbox);
            _log.Text = string.Join(LineBreak, run.Log);

            foreach (DialogueLine line in run.Lines)
                _lines.AddChild(Heading(Text(LineFormat, line.Speaker, Wording(line.TextKey))));

            foreach (DryRunOption option in run.Options)
                _options.AddChild(Pressable(option));
        }

        private string Idle()
        {
            if (_record is null) return NoRecordText;
            if (!_isDialogue) return NotADialogueText;

            return StaleText;
        }

        private Button Pressable(DryRunOption option)
        {
            var button = new Button
            {
                Text = Text(OptionFormat, Marks(option), option.Id, Wording(option.TextKey)),
                Disabled = !option.Visible || !option.Enabled,
                Alignment = HorizontalAlignment.Left
            };

            button.Pressed += () => Choose(option.Id);

            return button;
        }

        /// <summary>What the game would do with this option before its author ever presses it.</summary>
        private static string Marks(DryRunOption option)
        {
            string marks = string.Empty;

            if (!option.Visible) marks += HiddenMark;
            else if (!option.Enabled) marks += DisabledMark;

            if (option.SpeechCheckChance is { } chance) marks += Text(CheckFormat, Mathf.RoundToInt(chance * Percent));

            return marks;
        }

        /// <summary>The world the run has left behind, beside the one its author typed.</summary>
        private static string World(NarrativeSandbox sandbox) => string.Join(LineBreak,
        [
            Text(LineFormat, FactsTitle, string.Join(Separator, sandbox.Facts.Select(entry => Text(CountFormat, entry.Key, entry.Value)))),
            Text(LineFormat, ItemsTitle, string.Join(Separator, sandbox.Items.Select(entry => Text(CountFormat, entry.Key, entry.Value)))),
            Text(LineFormat, QuestsTitle, string.Join(Separator, sandbox.Quests.Select(state => Text(QuestStageFormat, state.QuestId, state.Status, state.StageId)))),
        ]);

        /// <summary>What a key says, out of the .po files the run is editing; the key itself while
        /// nobody has written a translation of it.</summary>
        private string Wording(string key)
        {
            if (Texts is not { } texts) return key;

            string? said = texts.Locales.Select(locale => texts.Read(locale, key)).FirstOrDefault(line => line is { Length: > 0 });

            return said is null ? key : Text(TranslatedFormat, key, said);
        }

    }

    /// <summary>A list of what an author has stated, each row with the one gesture that takes it back.</summary>
    public partial class DryRunRows : VBoxContainer
    {
        private const string RemoveText = "×";

        public event Action<string>? Removed;

        /// <summary>Empties a box of the rows it drew last time. Taken out of the tree before it is
        /// freed: the free itself waits for the end of the frame, and a row still standing there would
        /// be counted among the ones drawn next.</summary>
        public static void Clear(Node box)
        {
            ArgumentNullException.ThrowIfNull(box);

            foreach (Node child in box.GetChildren())
            {
                box.RemoveChild(child);
                child.QueueFree();
            }
        }

        public void Show(IReadOnlyList<(string Key, string Text)> rows)
        {
            ArgumentNullException.ThrowIfNull(rows);

            Clear(this);

            foreach ((string key, string text) in rows)
                AddChild(Row(key, text));
        }

        private HBoxContainer Row(string key, string text)
        {
            var remove = new Button { Text = RemoveText };
            remove.Pressed += () => Removed?.Invoke(key);

            var row = new HBoxContainer();
            row.AddChild(new Label { Text = text });
            row.AddChild(remove);

            return row;
        }
    }

    /// <summary>The members of one enum, read once. A generic static holds its own copy per enum, which
    /// is what makes 'read once' mean once per kind rather than once per call.</summary>
    internal static class EnumMembers<T> where T : struct, Enum
    {
        public static T[] All { get; } = Enum.GetValues<T>();
    }
}
