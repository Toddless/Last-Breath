namespace PassiveTreeEditor.Source.View
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Core.PassiveTree;
    using Godot;
    using Simulation;

    /// <summary>
    /// What the current allocation adds up to. Per parameter it shows the three buckets separately
    /// and then the value the game would actually resolve, so a suspicious total can be traced back
    /// to the bucket that produced it.
    /// </summary>
    public partial class SummaryPanel : VBoxContainer
    {
        private static readonly string[] s_headers = ["Parameter", "Flat", "Increase", "More", "Base", "Total"];

        public void Rebuild(TreeSummary summary, AllocationState allocation, int budget)
        {
            this.ClearContent();

            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            AddChild(EditorControls.Caption($"POINTS   spent {allocation.Spent} / {budget}   left {budget - allocation.Spent}"));

            if (summary.IsEmpty)
            {
                AddChild(EditorControls.Wrapped("Nothing allocated yet. Switch to Simulate mode and click a node next to a start point."));
                return;
            }

            BuildUnlocks(summary);
            BuildParameters(summary);
            BuildKeystones(summary);
        }

        private static string Signed(float value, bool percent)
        {
            if (Mathf.IsZeroApprox(value)) return "—";

            string body = percent
                ? EditorControls.Percent(value)
                : value.ToString("0.###", CultureInfo.InvariantCulture);

            return value > 0 ? "+" + body : body;
        }

        private static string Plain(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private void BuildUnlocks(TreeSummary summary)
        {
            // Sorted by class: a dictionary has no order of its own, and rows that swap places
            // between rebuilds read as if the allocation changed.
            foreach (KeyValuePair<PassiveNodeKind, List<string>> pair in summary.Unlocks.OrderBy(pair => pair.Key))
                AddChild(EditorControls.Wrapped($"{pair.Key} ({pair.Value.Count}): {string.Join(", ", pair.Value)}"));
        }

        private void BuildParameters(TreeSummary summary)
        {
            if (summary.Parameters.Count == 0) return;

            AddChild(EditorControls.Caption("PARAMETERS"));

            var grid = new GridContainer { Columns = s_headers.Length, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddChild(grid);

            foreach (string header in s_headers) grid.AddChild(EditorControls.Caption(header));

            foreach (ParameterTotal total in summary.Parameters)
            {
                // A conditional line is counted as if it were always on — the tool has no battle
                // state to test a condition against — so the row says how many of them there are.
                string name = total.Parameter.ToString();
                if (total.ConditionalLines > 0) name += $" ({total.ConditionalLines} cond.)";

                grid.AddChild(new Label { Text = name });
                grid.AddChild(new Label { Text = Signed(total.Flat, percent: false) });
                grid.AddChild(new Label { Text = Signed(total.Increase, percent: true) });
                grid.AddChild(new Label { Text = Signed(total.Multiplicative, percent: true) });
                grid.AddChild(new Label { Text = Plain(total.BaseValue) });
                grid.AddChild(new Label { Text = Plain(total.Total) });
            }

            if (summary.ConditionalLines > 0)
                AddChild(EditorControls.Wrapped($"{summary.ConditionalLines} conditional line(s) counted as always active."));
        }

        private void BuildKeystones(TreeSummary summary)
        {
            if (summary.Keystones.Count == 0) return;

            AddChild(EditorControls.Caption("KEYSTONES (not evaluated)"));
            foreach (string keystone in summary.Keystones) AddChild(EditorControls.Wrapped("• " + keystone));
        }
    }
}
