namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Validation;

    /// <summary>
    /// What the integrity rules found, as a list the author can walk. Every finding that belongs to a
    /// node is a button that jumps to it — a report naming a node the author then has to hunt for on a
    /// wheel of a couple of hundred is a report that does not get acted on. Findings about the tree
    /// itself have nowhere to jump and stay plain text.
    /// </summary>
    public partial class ValidationPanel : VBoxContainer
    {
        private Label _summary = null!;
        private VBoxContainer _rows = null!;

        /// <summary>The node behind a finding the author clicked.</summary>
        public event Action<string>? NodePicked;

        /// <summary>Run the rules again — the tree has moved on since the last report.</summary>
        public event Action? RecheckRequested;

        public void Initialize()
        {
            Name = "Check";
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 6);

            var recheck = new Button { Text = "Check now", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            recheck.Pressed += () => RecheckRequested?.Invoke();
            AddChild(recheck);

            _summary = new Label { Text = "not checked yet" };
            AddChild(_summary);

            _rows = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddChild(EditorControls.Scrolled(_rows));
        }

        /// <summary>Lays out a fresh report. Named away from <c>Show</c> on purpose: a method of that
        /// name would hide the inherited one for this type, and the next caller that means "make the
        /// panel visible" would be told the panel takes a list of findings.</summary>
        public void Report(IReadOnlyList<TreeIssue> issues)
        {
            _rows.ClearContent();
            _summary.Text = issues.Count == 0 ? "no problems found" : $"{issues.Count} problem(s)";

            foreach (TreeIssue issue in issues) _rows.AddChild(Row(issue));
        }

        /// <summary>Drops the report. Findings belong to the tree they were made on, and an empty list
        /// is not the same statement as a clean one — a tree nobody has checked says neither.</summary>
        public void Clear()
        {
            _rows.ClearContent();
            _summary.Text = "not checked yet";
        }

        private Control Row(TreeIssue issue) =>
            issue.HasNode
                ? EditorControls.RowButton(issue.Text, () => NodePicked?.Invoke(issue.NodeId))
                : EditorControls.Wrapped(issue.Text);
    }
}
