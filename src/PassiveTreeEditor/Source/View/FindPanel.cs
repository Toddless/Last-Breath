namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using Core.PassiveTree;
    using Godot;
    using Search;

    /// <summary>
    /// Finding a node by id, title or ability, and jumping to it. On a wheel of a couple of hundred
    /// nodes the only way to reach one by hand is to remember where it was drawn; this reaches it by
    /// what it is called. The matching itself is not here — the panel types into
    /// <see cref="NodeSearch"/> and lays out what comes back.
    /// </summary>
    public partial class FindPanel : VBoxContainer
    {
        /// <summary>How many hits get a row. A one-letter query answers with most of the tree, and
        /// rebuilding hundreds of rows on every keystroke is a stutter that makes the field feel broken.
        /// The count still reports the whole truth, and a query narrow enough to act on fits inside this
        /// several times over.</summary>
        private const int MaxRows = 60;

        private readonly List<string> _hits = [];

        private PassiveTreeDocument? _document;
        private LineEdit _query = null!;
        private Label _count = null!;
        private VBoxContainer _results = null!;

        /// <summary>The tree has moved on since the rows were built. Rebuilding is deferred to the
        /// moment the list is looked at: the panel shares a tab strip with the inspector, so nearly
        /// every edit that invalidates it happens while it is hidden.</summary>
        private bool _stale;

        /// <summary>A hit the author picked: the node to jump to.</summary>
        public event Action<string>? NodePicked;

        public void Initialize()
        {
            Name = "Find";
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 6);

            _query = new LineEdit
            {
                PlaceholderText = "id, title or ability",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                ClearButtonEnabled = true
            };

            _query.TextChanged += _ => Refresh();

            // Enter takes the best hit without reaching for the mouse: typing the id of a node and
            // pressing return is the fast path this panel exists for.
            _query.TextSubmitted += _ => PickFirst();
            AddChild(_query);

            _count = new Label { Text = "type to search" };
            AddChild(_count);

            _results = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddChild(EditorControls.Scrolled(_results));

            // Opening the tab is what pays for the rebuild the edits deferred.
            VisibilityChanged += RefreshIfStale;
        }

        /// <summary>A different tree, or the same one after an edit: the result list is only as good as
        /// the nodes it was built from, and a stale hit jumps to a node that is no longer there.</summary>
        public void SetDocument(PassiveTreeDocument document)
        {
            _document = document;
            Invalidate();
        }

        /// <summary>
        /// The nodes the rows were built from have changed. What a hit matches on — id, title, ability —
        /// is not what most edits touch, and the panel is not on screen for most of them either: a
        /// spin-box tick on a modifier would otherwise rebuild sixty buttons nobody is looking at. The
        /// list is brought up to date the moment it is shown, and immediately when it already is.
        /// </summary>
        public void Invalidate()
        {
            _stale = true;
            RefreshIfStale();
        }

        /// <summary>Hands the keyboard to the query field, with whatever is in it selected so the next
        /// keystroke starts a new search instead of extending the last one.</summary>
        public void FocusQuery()
        {
            _query.GrabFocus();
            _query.SelectAll();
        }

        private void PickFirst()
        {
            if (_hits.Count > 0) NodePicked?.Invoke(_hits[0]);
        }

        private void RefreshIfStale()
        {
            if (_stale && IsVisibleInTree()) Refresh();
        }

        /// <summary>Re-runs the current query against the tree as it stands now.</summary>
        public void Refresh()
        {
            _stale = false;
            _results.ClearContent();
            _hits.Clear();

            if (_document is null) return;

            if (_query.Text.Trim().Length == 0)
            {
                _count.Text = $"type to search — {_document.Nodes.Count} node(s)";
                return;
            }

            List<NodeSearchHit> hits = NodeSearch.Find(_document.Nodes, _query.Text);
            int shown = Math.Min(hits.Count, MaxRows);

            _count.Text = hits.Count == 0 ? "no match"
                : hits.Count > shown ? $"{hits.Count} match(es), showing the first {shown}"
                : $"{hits.Count} match(es)";

            for (int index = 0; index < shown; index++)
            {
                NodeSearchHit hit = hits[index];

                _hits.Add(hit.NodeId);
                _results.AddChild(EditorControls.RowButton(hit.Label, () => NodePicked?.Invoke(hit.NodeId)));
            }
        }
    }
}
