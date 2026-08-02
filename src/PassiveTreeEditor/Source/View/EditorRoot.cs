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
    using Godot;
    using Io;
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
        private AcceptDialog _messageDialog = null!;
        private FileDialog _fileDialog = null!;
        private NodeTooltip _tooltip = null!;
        private bool _dirty;

        public override void _Ready()
        {
            _settings = EditorSettings.Load();

            BuildUi();
            WireEvents();
            LoadGameData();
        }

        public override void _ExitTree()
        {
            CaptureSettings();
            EditorSettings.Save(_settings);
        }

        /// <summary>Ctrl+F opens the search from wherever the hands already are. Shortcut input rather
        /// than unhandled input: it runs while a text field holds the keyboard, which is exactly when
        /// the author wants to search for the next node.</summary>
        public override void _ShortcutInput(InputEvent @event)
        {
            if (@event is not InputEventKey { Pressed: true, Keycode: Key.F, CtrlPressed: true }) return;

            OpenFind();
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
            bar.AddChild(ToolbarButton("New", NewTree));
            bar.AddChild(ToolbarButton("Frame", () => _canvas.FrameAll()));

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
                _canvas.Document.Budget = (int)value;
                _canvas.RefreshFrontier();
                RefreshSummary();
            };
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
            _inspector.Initialize(_canvas, _abilities, _conditions);

            _canvas.SelectionChanged += () => _inspector.Rebuild();
            _canvas.DocumentChanged += OnDocumentChanged;
            _canvas.AllocationChanged += RefreshSummary;
            _canvas.StatusChanged += SetStatus;
            _canvas.HoveredChanged += OnHovered;
            _inspector.NodeEdited += MarkDirty;
            _inspector.TotalsChanged += OnDocumentChanged;
            _baseStats.ProfileChanged += RefreshSummary;
            _find.NodePicked += JumpTo;
            _check.NodePicked += JumpTo;
            _check.RecheckRequested += RunValidation;
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

        private void AdoptDocument(PassiveTreeDocument document)
        {
            _allocation.Reset(document);
            _canvas.SetDocument(document, _allocation);
            _budget.Value = document.Budget;
            _dirty = false;
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

            _dirty = false;
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

        private void MarkDirty()
        {
            _dirty = true;

            // The title is one of the three fields a search matches on, and it is edited from here.
            _find.Invalidate();
            SetStatus("editing");
        }

        private void OnDocumentChanged()
        {
            _dirty = true;
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
            _status.Text = _dirty ? message + "   •  unsaved changes" : message;

        private void ShowMessage(string title, IReadOnlyList<string> lines)
        {
            _messageDialog.Title = title;
            _messageDialog.DialogText = string.Join("\n", lines);
            _messageDialog.PopupCentered(new Vector2I(760, 520));
        }
    }
}
