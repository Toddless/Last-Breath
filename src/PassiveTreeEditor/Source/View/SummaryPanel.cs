namespace PassiveTreeEditor.Source.View
{
    using System.Globalization;
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
            foreach (Node child in GetChildren())
            {
                RemoveChild(child);
                child.QueueFree();
            }

            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            AddChild(Caption($"POINTS   spent {allocation.Spent} / {budget}   left {budget - allocation.Spent}"));

            if (summary.IsEmpty)
            {
                AddChild(Wrapped("Nothing allocated yet. Switch to Simulate mode and click a node next to a start point."));
                return;
            }

            BuildUnlocks(summary);
            BuildParameters(summary);
            BuildKeystones(summary);
        }

        private static Label Caption(string text)
        {
            var label = new Label { Text = text };
            label.AddThemeFontSizeOverride("font_size", 13);
            return label;
        }

        private static Label Wrapped(string text) => new()
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        private static string Signed(float value, bool percent)
        {
            if (Mathf.IsZeroApprox(value)) return "—";

            string body = percent
                ? (value * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%"
                : value.ToString("0.###", CultureInfo.InvariantCulture);

            return value > 0 ? "+" + body : body;
        }

        private static string Plain(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private void BuildUnlocks(TreeSummary summary)
        {
            if (summary.Abilities.Count > 0)
                AddChild(Wrapped($"Abilities ({summary.Abilities.Count}): {string.Join(", ", summary.Abilities)}"));

            if (summary.SocketsTier2.Count > 0)
                AddChild(Wrapped($"T2 sockets ({summary.SocketsTier2.Count}): {string.Join(", ", summary.SocketsTier2)}"));

            if (summary.SocketsTier3.Count > 0)
                AddChild(Wrapped($"T3 sockets ({summary.SocketsTier3.Count}): {string.Join(", ", summary.SocketsTier3)}"));
        }

        private void BuildParameters(TreeSummary summary)
        {
            if (summary.Parameters.Count == 0) return;

            AddChild(Caption("PARAMETERS"));

            var grid = new GridContainer { Columns = s_headers.Length, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddChild(grid);

            foreach (string header in s_headers) grid.AddChild(Caption(header));

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
                AddChild(Wrapped($"{summary.ConditionalLines} conditional line(s) counted as always active."));
        }

        private void BuildKeystones(TreeSummary summary)
        {
            if (summary.Keystones.Count == 0) return;

            AddChild(Caption("KEYSTONES (not evaluated)"));
            foreach (string keystone in summary.Keystones) AddChild(Wrapped("• " + keystone));
        }
    }
}
