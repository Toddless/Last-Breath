namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Enums;
    using Core.Localization;
    using Core.Modifiers.Conditions;
    using Core.PassiveTree;
    using Editing;
    using Editing.History;
    using Godot;
    using Io;
    using Navigation;
    using Simulation;
    using Validation;
    using EditorSettings = Io.EditorSettings;

    /// <summary>
    /// Composition root of the tool: builds the layout in code, loads game data through the game's
    /// own data-catalog pipeline, and wires the canvas to the panels. The scene file holds nothing
    /// but this node — there is no designer to hand a .tscn to, so the layout lives where it can be
    /// reviewed as code.
    /// </summary>
    public partial class EditorRoot : Control
    {
        private readonly AbilityCatalog _abilities = new();
        private readonly PassiveTreeProvider _treeProvider = new();
        private readonly AllocationState _allocation = new();

        /// <summary>The document and the stack of what has been done to it. Owned here because both the
        /// canvas and the inspector edit the same tree and both have to be undone from one place.</summary>
        private readonly TreeEditor _editor = new();

        // The game's own condition catalog, read through the same participant contract as everything
        // else here: the ids a line may name are the ids the game will resolve, with no list of them
        // kept in the tool to fall out of date.
        private readonly ConditionProvider _conditions = new(ConditionParser.Default());

        // The game's own baseline reader (Core): the tool measures the tree on the numbers the player
        // actually starts with, and a bad profile is reported through the game's Tracker, not here.
        private readonly PlayerStatsProvider _playerStats = new();

        // The game's own display units for parameters, read from the Formatting catalog — the tooltip
        // must not decide on its own what counts as a percent.
        private readonly ParameterFormatProvider _parameterFormats = new();
        private readonly GodotLocalizationProvider _localization = new();

        private EditorSettingsState _settings = new();
        private TreeCanvas _canvas = null!;
        private InspectorPanel _inspector = null!;
        private SummaryPanel _summary = null!;
        private BaseStatsPanel _baseStats = null!;
        private FindPanel _find = null!;
        private ValidationPanel _check = null!;
        private TabContainer _tabs = null!;
        private LineEdit _pathEdit = null!;
        private Label _status = null!;
        private SpinBox _budget = null!;
        private SpinBox _spread = null!;
        private AcceptDialog _messageDialog = null!;
        private FileDialog _fileDialog = null!;
        private NodeTooltip _tooltip = null!;
        private Button _undoButton = null!;
        private Button _redoButton = null!;

        /// <summary>Whether the tree on screen differs from the file. Asked of the history rather than
        /// kept as a flag: stepping back to exactly what was saved makes the tree saved again, and a
        /// flag that only ever turns on would keep reporting changes that no longer exist.</summary>
        private bool Dirty => !_editor.History.IsClean;

        public override void _Ready()
        {
            _settings = EditorSettings.Load();

            BuildUi();
            WireEvents();

            // Through the control rather than the canvas, so the box and the view start out agreeing.
            // Before the tree is read: the first frame has to fit the tree at the spread it will be
            // read at, not at the one it happened to start with.
            _spread.Value = _settings.LayoutSpread;

            LoadGameData();
        }

        public override void _ExitTree()
        {
            CaptureSettings();
            EditorSettings.Save(_settings);
        }

        /// <summary>
        /// The tool-wide shortcuts, answered ahead of every panel and every text field.
        /// <para>Plain input rather than shortcut input, which is where the search key used to live: a
        /// focused text field swallows Ctrl+Z for its own character-level undo long before shortcut
        /// input is reached, and a tool whose undo means "one letter" while the caret is in a title and
        /// "one edit" everywhere else is a tool whose undo nobody can predict. The tool has one history
        /// and one key for it; a title typed into a node is a step in that history like any other.</para>
        /// <para>Ctrl+Z steps back, Ctrl+Y and Ctrl+Shift+Z both step forward — both are muscle memory
        /// and neither has anything else to mean here. Held keys repeat and history steps deliberately
        /// do not follow the repeat: one press is one step, so leaning on the key cannot unwind a
        /// session faster than it can be read.</para>
        /// </summary>
        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventKey { Pressed: true, Echo: false, CtrlPressed: true } key) return;

            switch (key.Keycode)
            {
                case Key.F:
                    OpenFind();
                    break;
                case Key.Z when key.ShiftPressed:
                case Key.Y:
                    StepForward();
                    break;
                case Key.Z:
                    StepBack();
                    break;
                default:
                    return;
            }

            GetViewport().SetInputAsHandled();
        }

        // ── layout ─────────────────────────────────────────────────────────────────────────────

        private void BuildUi()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);

            var root = new VBoxContainer();
            root.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(root);

            _canvas = new TreeCanvas
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            root.AddChild(BuildToolbar());

            var body = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            root.AddChild(body);
            body.AddChild(_canvas);
            body.AddChild(BuildSidePanel());

            _status = new Label { Text = "ready" };
            root.AddChild(_status);

            _messageDialog = new AcceptDialog();
            AddChild(_messageDialog);

            _fileDialog = new FileDialog
            {
                FileMode = FileDialog.FileModeEnum.SaveFile,
                Access = FileDialog.AccessEnum.Filesystem,
                Title = "Tree file",
                Filters = ["*.json ; JSON"]
            };
            _fileDialog.FileSelected += path => _pathEdit.Text = path;
            AddChild(_fileDialog);

            // Added last so it draws over the canvas and the side panel.
            _tooltip = new NodeTooltip();
            AddChild(_tooltip);
            _tooltip.Initialize(
                new ModifierFormatter(_localization, _parameterFormats),
                new ContextModifierFormatter(_localization),
                _localization);
        }

        private Control BuildToolbar()
        {
            var bar = new HBoxContainer();
            var group = new ButtonGroup();

            foreach (EditorMode mode in Enum.GetValues<EditorMode>())
            {
                var button = new Button
                {
                    Text = mode.ToString(),
                    ToggleMode = true,
                    ButtonGroup = group
                };

                if (mode == EditorMode.Select) button.ButtonPressed = true;

                EditorMode captured = mode;
                button.Pressed += () => SetMode(captured);
                bar.AddChild(button);
            }

            bar.AddChild(new VSeparator());

            _pathEdit = new LineEdit
            {
                Text = string.IsNullOrWhiteSpace(_settings.TreePath) ? ToolPaths.DefaultTreePath : _settings.TreePath,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(320, 0)
            };
            bar.AddChild(_pathEdit);

            bar.AddChild(ToolbarButton("…", () => _fileDialog.PopupCentered(new Vector2I(900, 600))));
            bar.AddChild(ToolbarButton("Load", LoadTree));
            bar.AddChild(ToolbarButton("Save", SaveTree));
            bar.AddChild(new VSeparator());

            // The shortcuts do the work; the buttons are how the author sees that there is anything to
            // go back to, and what the next step back would be.
            _undoButton = ToolbarButton("Undo", StepBack);
            _redoButton = ToolbarButton("Redo", StepForward);
            bar.AddChild(_undoButton);
            bar.AddChild(_redoButton);
            bar.AddChild(new VSeparator());

            bar.AddChild(ToolbarButton("New", NewTree));
            bar.AddChild(ToolbarButton("Frame", () => _canvas.FrameAll()));
            bar.AddChild(SpreadBox());

            var guides = new Button { Text = "Guides", ToggleMode = true, ButtonPressed = true };
            guides.Toggled += pressed =>
            {
                _canvas.ShowGuides = pressed;
                _canvas.QueueRedraw();
            };
            bar.AddChild(guides);

            bar.AddChild(ToolbarButton("Find", OpenFind));
            bar.AddChild(ToolbarButton("Check", RunValidation));

            return bar;
        }

        private static Button ToolbarButton(string text, Action action)
        {
            var button = new Button { Text = text };
            button.Pressed += action;
            return button;
        }

        /// <summary>
        /// The layout spread, in the header next to Frame because it is the same kind of handle: both
        /// change how the tree is looked at, neither changes what is in it. It reads as a number rather
        /// than a slider — being back at exactly 1 is the one position that has to be recognisable, and
        /// "somewhere near the left end" is not recognising it.
        /// </summary>
        private Control SpreadBox()
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = "spread" });

            _spread = new SpinBox
            {
                MinValue = CanvasTransform.MinSpread,
                MaxValue = CanvasTransform.MaxSpread,
                Step = CanvasTransform.SpreadStep,
                Value = CanvasTransform.DefaultSpread,
                TooltipText = "distance between nodes — [ and ] step it on the canvas. "
                              + "Nodes keep their size; 1 is the layout as it is written in the file"
            };

            _spread.ValueChanged += value => _canvas.LayoutSpread = (float)value;
            row.AddChild(_spread);

            return row;
        }

        private Control BuildSidePanel()
        {
            var side = new VBoxContainer
            {
                CustomMinimumSize = new Vector2(430, 0),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            var simulation = new HBoxContainer();
            simulation.AddChild(new Label { Text = "budget" });

            _budget = new SpinBox
            {
                MinValue = 0,
                MaxValue = 999,
                Step = 1,
                Value = PassiveTreeDocument.DefaultBudget
            };
            _budget.ValueChanged += value =>
            {
                _editor.SetBudget((int)value);
                _canvas.RefreshFrontier();
                RefreshSummary();
            };

            // Leaving the box ends the run of keystrokes it was taking, the same way leaving a text
            // field does — the spin box keeps its number in a line edit of its own, and that is the
            // control the focus actually belongs to.
            _budget.GetLineEdit().FocusExited += _editor.Seal;
            simulation.AddChild(_budget);

            var reset = new Button { Text = "Reset allocation" };
            reset.Pressed += () =>
            {
                _allocation.Reset(_canvas.Document);
                _canvas.RefreshFrontier();
                RefreshSummary();
                SetStatus("allocation reset to the start points");
            };
            simulation.AddChild(reset);
            side.AddChild(simulation);

            _tabs = new TabContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            side.AddChild(_tabs);

            _inspector = new InspectorPanel();
            _summary = new SummaryPanel();
            _baseStats = new BaseStatsPanel();
            _find = new FindPanel();
            _check = new ValidationPanel();
            _find.Initialize();
            _check.Initialize();

            _tabs.AddChild(Scrolled("Node", _inspector));
            _tabs.AddChild(Scrolled("Totals", _summary));
            _tabs.AddChild(Scrolled("Base", _baseStats));

            // The two result panels scroll their own list and go in whole rather than wrapped: the
            // query field and the count stay put while the results move under them.
            _tabs.AddChild(_find);
            _tabs.AddChild(_check);

            return side;
        }

        private static ScrollContainer Scrolled(string name, Control content)
        {
            ScrollContainer scroll = EditorControls.Scrolled(content);
            scroll.Name = name;
            return scroll;
        }

        private void WireEvents()
        {
            _canvas.Initialize(_editor);
            _inspector.Initialize(_canvas, _editor, _abilities, _conditions);

            _editor.Restored += OnHistoryStep;
            _editor.History.Changed += RefreshHistoryButtons;

            _canvas.SelectionChanged += OnSelectionChanged;
            _canvas.DocumentChanged += OnDocumentChanged;
            _canvas.AllocationChanged += RefreshSummary;
            _canvas.StatusChanged += SetStatus;
            _canvas.HoveredChanged += OnHovered;

            // The keys are the other way to move the same handle, so the box follows them. Without the
            // signal, because the box is not being told anything it did not already know.
            _canvas.SpreadChanged += spread => _spread.SetValueNoSignal(spread);
            _inspector.NodeEdited += OnNodeEdited;
            _inspector.TotalsChanged += OnDocumentChanged;
            _baseStats.ProfileChanged += RefreshSummary;
            _find.NodePicked += JumpTo;
            _check.NodePicked += JumpTo;
            _check.RecheckRequested += RunValidation;

            RefreshHistoryButtons();
        }

        /// <summary>Both result lists lead to the same place: the node in the middle of the canvas,
        /// selected, with the inspector already showing it.</summary>
        private void JumpTo(string id)
        {
            if (_canvas.FocusNode(id)) SetStatus($"jumped to {id}");
            else SetStatus($"{id} is no longer in the tree");
        }

        // ── data ───────────────────────────────────────────────────────────────────────────────

        private void LoadGameData()
        {
            var failures = new List<string>();
            var source = new FileSystemDataSource(ToolPaths.SharedDataRoot);
            var participants = new IGameDataParticipant[] { _abilities, _treeProvider, _playerStats, _parameterFormats, _conditions };
            var service = new GameDataService(source, participants);

            service.LoadFailed += (catalog, exception) => failures.Add($"{catalog}: {exception.Message}");
            service.LoadAll();

            ApplyBaseStats();

            // A custom path from the last session wins over the catalog copy; otherwise the tree the
            // data pipeline just read is the document.
            PassiveTreeDocument document = _treeProvider.Tree;
            List<string> issues = [.. _treeProvider.Issues];

            if (!string.IsNullOrWhiteSpace(_settings.TreePath) && System.IO.File.Exists(_settings.TreePath))
            {
                issues.Clear();
                document = PassiveTreeSerializer.Load(_settings.TreePath, issues);
            }

            AdoptDocument(document);
            _canvas.FrameAll();

            SetStatus($"{_abilities.Abilities.Count} abilities, {document.Nodes.Count} nodes"
                      + (failures.Count > 0 ? $", {failures.Count} catalog(s) unavailable" : string.Empty)
                      + (issues.Count > 0 ? $", {issues.Count} data issue(s)" : string.Empty));

            if (issues.Count > 0) ShowMessage("Data issues", issues);
        }

        /// <summary>Settings are read before any catalog exists, so the profile can only be assembled
        /// here: the data baseline first, then whatever the last session edited on top of it.</summary>
        private void ApplyBaseStats()
        {
            BaseStatProfile baseline = BaseStatProfile.From(_playerStats.Unarmed);
            BaseStatProfile profile = baseline.Copy();

            foreach (KeyValuePair<EntityParameter, float> pair in _settings.BaseStats.Values)
                profile[pair.Key] = pair.Value;

            _baseStats.Initialize(profile, baseline);
        }

        /// <summary>
        /// Another tree becomes the one being edited. The history is dropped with it (the editor does
        /// that when it takes the document): its steps hold nodes of the tree just closed, and a step
        /// back into a file that is no longer open is exactly the wrong restore this layer exists to
        /// prevent. Loading and starting a new tree are therefore not undoable, on purpose.
        /// </summary>
        private void AdoptDocument(PassiveTreeDocument document)
        {
            _allocation.Reset(document);
            _editor.SetDocument(document);
            _canvas.DocumentReplaced(_allocation);
            _budget.Value = document.Budget;
            _inspector.Rebuild();
            _find.SetDocument(document);

            // Findings belong to the tree they were made on, so a different tree starts unchecked.
            _check.Clear();
            RefreshSummary();
        }

        private void NewTree()
        {
            AdoptDocument(new PassiveTreeDocument { Budget = (int)_budget.Value });
            _canvas.FrameAll();
            SetStatus("new empty tree");
        }

        private void LoadTree()
        {
            List<string> issues = [];
            PassiveTreeDocument document = PassiveTreeSerializer.Load(_pathEdit.Text.Trim(), issues);

            AdoptDocument(document);
            _canvas.FrameAll();

            SetStatus(issues.Count == 0
                ? $"loaded {document.Nodes.Count} node(s)"
                : $"loaded {document.Nodes.Count} node(s) with {issues.Count} issue(s)");

            if (issues.Count > 0) ShowMessage("Load issues", issues);
        }

        private void SaveTree()
        {
            CaptureSettings();
            string path = _settings.TreePath;

            try
            {
                PassiveTreeSerializer.Save(_canvas.Document, path);
            }
            catch (Exception exception)
            {
                ShowMessage("Save failed", [exception.Message]);
                return;
            }

            // The state now on disk. Steps taken from here are what "unsaved changes" means, and
            // stepping back onto this one means there is nothing left to save.
            _editor.History.MarkSaved();
            EditorSettings.Save(_settings);
            SetStatus($"saved {_canvas.Document.Nodes.Count} node(s) to {path}");
        }

        /// <summary>The single place the settings snapshot is assembled. "Reset to unarmed" hands the
        /// panel a brand-new profile instance, so anything holding on to the old one goes stale without
        /// a word — both save paths have to re-read the live values instead of trusting an alias.</summary>
        private void CaptureSettings()
        {
            _settings.TreePath = _pathEdit.Text.Trim();
            _settings.BaseStats = _baseStats.Overrides();

            // Read off the canvas, not off the box: the keys move the canvas first and the box after.
            _settings.LayoutSpread = _canvas.LayoutSpread;
        }

        /// <summary>
        /// Runs the integrity rules and shows what they found. The report is a panel rather than a
        /// dialog because a finding is a place to go: the author reads a line, clicks it, fixes the node
        /// and reads the next one, and a modal list would be dismissed before the first fix.
        /// </summary>
        private void RunValidation()
        {
            List<TreeIssue> issues = TreeValidator.Validate(_canvas.Document, AbilityView());

            _check.Report(issues);
            ShowTab(_check);
            SetStatus(issues.Count == 0 ? "check: no problems found" : $"check: {issues.Count} problem(s)");
        }

        /// <summary>What the tree may name, and what it may name without being wrong: the ability rules
        /// tell an id nobody has heard of apart from one that exists but never belongs in a tree.</summary>
        private AbilityCatalogView AbilityView() =>
            new(_abilities.Abilities.Select(entry => entry.Id), _abilities.Selectable().Select(entry => entry.Id));

        private void OpenFind()
        {
            ShowTab(_find);
            _find.FocusQuery();
        }

        private void ShowTab(Control panel) => _tabs.CurrentTab = _tabs.GetTabIdxFromControl(panel);

        // ── glue ───────────────────────────────────────────────────────────────────────────────

        private void SetMode(EditorMode mode)
        {
            _canvas.Mode = mode;

            if (mode == EditorMode.Simulate)
            {
                _allocation.Resync(_canvas.Document);
                _canvas.RefreshFrontier();
                RefreshSummary();
            }

            _canvas.QueueRedraw();
            SetStatus($"{mode} mode");
        }

        private void OnHovered(PassiveNode? node)
        {
            if (node is null) _tooltip.HideTip();
            else _tooltip.ShowFor(node, GetGlobalMousePosition());
        }

        private void OnNodeEdited()
        {
            // The title is one of the three fields a search matches on, and it is edited from here.
            _find.Invalidate();
            SetStatus("editing");
        }

        /// <summary>Moving to another node ends whatever run of keystrokes the last one was taking, so
        /// the title of one node and the title of the next are two steps and not one.</summary>
        private void OnSelectionChanged()
        {
            _editor.Seal();
            _inspector.Rebuild();
        }

        private void StepBack() => Report("undo", _editor.Undo());

        private void StepForward() => Report("redo", _editor.Redo());

        private void Report(string direction, string? step) =>
            SetStatus(step is null ? $"nothing to {direction}" : $"{direction}: {step}");

        /// <summary>
        /// What the tool shows is rebuilt from the document after every step through the history. Every
        /// panel holds values copied out of the tree, and a control still showing what was typed a
        /// moment ago is the exact confusion undo exists to prevent — the author would be looking at
        /// one number and saving another. The rebuild costs the focus, so the caret leaves the field it
        /// was in: a step is a step of the tool, not of the field.
        /// <para>The selection goes first and takes the step with it: a step that renamed a node has to
        /// leave that node selected under the id it now has, or the panel answers "nothing selected" to
        /// an undo whose whole subject is the id.</para>
        /// </summary>
        private void OnHistoryStep(HistoryStep step)
        {
            _canvas.SyncSelection(step.Rename);
            _allocation.Resync(_canvas.Document);
            _canvas.RefreshFrontier();
            _budget.Value = _canvas.Document.Budget;
            _inspector.Rebuild();
            _find.Invalidate();
            RefreshSummary();
            _canvas.QueueRedraw();
        }

        /// <summary>The buttons say what the stack holds. A disabled Undo is the honest answer to
        /// "can I take that back" at the point where the answer is no.</summary>
        private void RefreshHistoryButtons()
        {
            EditHistory history = _editor.History;

            _undoButton.Disabled = !history.CanUndo;
            _redoButton.Disabled = !history.CanRedo;
            _undoButton.TooltipText = history.NextUndo ?? "nothing to undo";
            _redoButton.TooltipText = history.NextRedo ?? "nothing to redo";
        }

        private void OnDocumentChanged()
        {
            _allocation.Resync(_canvas.Document);
            _canvas.RefreshFrontier();

            // A hit that no longer exists jumps nowhere, and a node just created is the one most likely
            // to be searched for next.
            _find.Invalidate();
            RefreshSummary();
        }

        private void RefreshSummary()
        {
            TreeSummary summary = StatSummary.Build(_canvas.Document, _allocation, _baseStats.Profile);
            _summary.Rebuild(summary, _allocation, _canvas.Document.Budget);
        }

        private void SetStatus(string message) =>
            _status.Text = Dirty ? message + "   •  unsaved changes" : message;

        private void ShowMessage(string title, IReadOnlyList<string> lines)
        {
            _messageDialog.Title = title;
            _messageDialog.DialogText = string.Join("\n", lines);
            _messageDialog.PopupCentered(new Vector2I(760, 520));
        }
    }
}
