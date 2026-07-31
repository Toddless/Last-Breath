namespace PassiveTreeEditor.Source.View
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Core.Modifiers.Context;
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

        private static readonly string[] s_contextHeaders = ["Knob", "Written as", "Total", "Lines"];

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
            BuildContext(summary);
            BuildKeystones(summary);

            // Both channels feed this count, so it lives outside either table — a tree of context-only
            // conditionals must not lose the warning with the parameter grid it never had.
            if (summary.ConditionalLines > 0)
                AddChild(EditorControls.Wrapped($"{summary.ConditionalLines} conditional line(s) counted as always active."));
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
        }

        /// <summary>
        /// Pipeline knobs in their own table, one row per knob holding everything taken for it — that is
        /// the one number a fighter carries, whichever buckets the lines behind it were written in. They
        /// are never folded into the parameter totals: nothing about a knob passes through the parameter
        /// formula, and a row in that table would claim otherwise.
        /// <para>The total is printed bare. Knobs have no entry in the parameter format catalog and the
        /// bucket of a line governs no more than the wording of that line's own sentence, so dressing the
        /// sum in a unit here would be the panel inventing one: what the number means is the binding's
        /// business, and the row says what the binding will be handed.</para>
        /// </summary>
        private void BuildContext(TreeSummary summary)
        {
            if (summary.Context.Count == 0) return;

            AddChild(EditorControls.Caption("CONTEXT KNOBS (battle pipelines, outside parameter math)"));

            var grid = new GridContainer { Columns = s_contextHeaders.Length, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddChild(grid);

            foreach (string header in s_contextHeaders) grid.AddChild(EditorControls.Caption(header));

            foreach (ContextTotal total in summary.Context)
            {
                string name = total.Parameter.ToString();
                if (total.ConditionalLines > 0) name += $" ({total.ConditionalLines} cond.)";

                // Switch-ness comes off the binding table, like everywhere else a knob's kind is needed:
                // a switch carries no number for the row to print, however its lines were written.
                bool isFlag = ContextKnobs.IsFlag(total.Parameter);

                grid.AddChild(new Label { Text = name });
                grid.AddChild(new Label { Text = total.Bucket?.ToString() ?? "mixed" });
                grid.AddChild(new Label { Text = isFlag ? "on" : Signed(total.Value, percent: false) });
                grid.AddChild(new Label { Text = total.Lines.ToString(CultureInfo.InvariantCulture) });
            }
        }

        private void BuildKeystones(TreeSummary summary)
        {
            if (summary.Keystones.Count == 0) return;

            AddChild(EditorControls.Caption("KEYSTONES (not evaluated)"));
            foreach (string keystone in summary.Keystones) AddChild(EditorControls.Wrapped("• " + keystone));
        }
    }
}
