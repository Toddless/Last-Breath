namespace DataEditor.Source.View
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using App;
    using Godot;
    using Io;
    using Tooling.Catalogs;

    /// <summary>
    /// Composition root of the data editor: three panes built in code — the catalogs the game
    /// describes, the records of the chosen one, and the record itself — over a status line naming
    /// what was read. The scene file holds nothing but this node; there is no designer to hand a .tscn
    /// to, so the layout lives where it can be reviewed as code.
    /// <para>This build only reads. Everything on screen comes from a schema and a json document, and
    /// the writing half is a later step.</para>
    /// </summary>
    public partial class EditorRoot : Control
    {
        private const int CatalogPaneWidth = 220;
        private const int RecordPaneWidth = 260;
        private const int DialogWidth = 760;
        private const int DialogHeight = 520;

        /// <summary>What a selection index means when there is nothing to select.</summary>
        private const int NoSelection = -1;

        private const string CatalogRowFormat = "{0}   ({1})";
        private const string RecordRowFormat = "{0}   ·  {1}";
        private const string StatusFormat =
            "{0}   —   {1} catalog(s) of {2} described, {3} file(s), {4} record(s), {5} note(s)";

        private const string NoRootTitle = "Data root not found";
        private const string NoRootHint = "Data/Shared is a symlink to src/SharedData; restore-links.ps1 puts it back.";
        private const string IssuesTitle = "Data issues";

        private ItemList _catalogList = null!;
        private ItemList _recordList = null!;
        private InspectorPanel _inspector = null!;
        private Label _status = null!;
        private AcceptDialog _messageDialog = null!;

        private CatalogWorkspace? _workspace;
        private CatalogView? _catalog;

        public override void _Ready()
        {
            BuildUi();
            LoadCatalogs();
        }

        private void BuildUi()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);

            var root = new VBoxContainer();
            root.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(root);

            _catalogList = new ItemList
            {
                CustomMinimumSize = new Vector2(CatalogPaneWidth, 0),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            _recordList = new ItemList
            {
                CustomMinimumSize = new Vector2(RecordPaneWidth, 0),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            _inspector = new InspectorPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            // Two divides rather than one container holding all three panes: nested, each divider
            // starts at the minimum width of the pane before it and moves without touching the other.
            HSplitContainer body = Split();
            HSplitContainer right = Split();

            body.AddChild(_catalogList);
            body.AddChild(right);
            right.AddChild(_recordList);
            right.AddChild(Scrolled(_inspector));
            root.AddChild(body);

            _status = new Label();
            root.AddChild(_status);

            _messageDialog = new AcceptDialog();
            AddChild(_messageDialog);

            _catalogList.ItemSelected += index => ShowCatalog((int)index);
            _recordList.ItemSelected += index => ShowRecord((int)index);
        }

        /// <summary>A resizable divide. The offset is left at zero, which puts the divider at the end of
        /// the first pane's minimum width — the width each list already declares, written down once.</summary>
        private static HSplitContainer Split() => new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        /// <summary>
        /// A vertically scrolling frame around content that grows. Horizontal scrolling is off on
        /// purpose: it is what fixes the width of the content, and wrapped text needs a width before it
        /// can report how tall it is.
        /// </summary>
        private static ScrollContainer Scrolled(Control content)
        {
            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
            };

            scroll.AddChild(content);

            return scroll;
        }

        /// <summary>
        /// Reads every described catalog from the data root of this run and shows the first of them.
        /// A root that is not there at all is said on its own: every catalog under it is missing for
        /// that one reason, and a list of them names the symptom instead of the cause.
        /// </summary>
        private void LoadCatalogs()
        {
            string root = ToolPaths.SharedDataRoot;

            _workspace = GameCatalogs.Load(root);
            _catalogList.Clear();

            foreach (CatalogView view in _workspace.Catalogs)
                _catalogList.AddItem(Text(CatalogRowFormat, view.Catalog, view.Records.Count));

            _status.Text = Text(
                StatusFormat,
                root,
                _workspace.Catalogs.Count,
                _workspace.Catalogs.Count + GameCatalogs.UndescribedCount,
                _workspace.FileCount,
                _workspace.RecordCount,
                _workspace.Report.Count);

            ShowCatalog(_workspace.Catalogs.Count > 0 ? 0 : NoSelection);

            if (!Directory.Exists(root)) ShowMessage(NoRootTitle, [root, NoRootHint]);
            else if (_workspace.Report.Count > 0) ShowMessage(IssuesTitle, _workspace.Report);
        }

        /// <summary>Shows the records of one catalog, and the first of them. Selecting the row in the
        /// list as well, because the catalog is also chosen without a click — the tool opens on one.</summary>
        private void ShowCatalog(int index)
        {
            _catalog = _workspace is not null && index >= 0 && index < _workspace.Catalogs.Count
                ? _workspace.Catalogs[index]
                : null;

            if (_catalog is not null) _catalogList.Select(index);

            _recordList.Clear();

            if (_catalog is not null)
                foreach (CatalogRecord record in _catalog.Records)
                    _recordList.AddItem(RecordRow(record));

            ShowRecord(_catalog is { Records.Count: > 0 } ? 0 : NoSelection);
        }

        /// <summary>A record is named by its id, and by its file too when the catalog is written across
        /// several — two records of one catalog may share an id only because they sit in different
        /// files, and the list has to be able to say which is which.</summary>
        private string RecordRow(CatalogRecord record) =>
            _catalog is { Files.Count: > 1 } ? Text(RecordRowFormat, record.Id, record.File.Name) : record.Id;

        private void ShowRecord(int index)
        {
            CatalogRecord? record = _catalog is not null && index >= 0 && index < _catalog.Records.Count
                ? _catalog.Records[index]
                : null;

            if (record is not null) _recordList.Select(index);

            _inspector.Rebuild(record);
        }

        private void ShowMessage(string title, IReadOnlyList<string> lines)
        {
            _messageDialog.Title = title;
            _messageDialog.DialogText = string.Join("\n", lines);
            _messageDialog.PopupCentered(new Vector2I(DialogWidth, DialogHeight));
        }

        private static string Text(string format, params object?[] parts) =>
            string.Format(CultureInfo.InvariantCulture, format, parts);
    }
}
