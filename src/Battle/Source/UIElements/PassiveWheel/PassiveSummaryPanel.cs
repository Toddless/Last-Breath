namespace Battle.Source.UIElements.PassiveWheel
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Localization;
    using Core.Modifiers;
    using Core.PassiveTree;
    using Core.PassiveTree.Summary;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// What the tree has actually given the character, added up: parameters, pipeline knobs, keystones
    /// and the abilities his nodes unlocked. What replaced the column of node cards — a player plans
    /// against a total, not against one node at a time.
    ///
    /// <para>Gated lines are LEFT OUT of every number and only counted, and so are lines worth their value
    /// per unit of a parameter — this panel has no fighter to measure one against. A total a player reads as
    /// his own must not carry a bonus that is off while he reads it, nor a number nobody holds; how much of
    /// the allocation is not being shown is said at the bottom.</para>
    ///
    /// <para>Measured against <see cref="NoBaseline"/> on purpose: the panel shows the CONTRIBUTION OF THE
    /// TREE, and the tree's contribution is already registered on the living character — reading it
    /// against him would count every line twice.</para>
    /// </summary>
    [GlobalClass]
    public partial class PassiveSummaryPanel : PanelContainer
    {
        /// <summary>The uid the scene file declares in its own header — the authority, so a copy that
        /// drifts from it resolves to nothing at all in the game project.</summary>
        private const string UID = "uid://bsp5mq3vhtx2n";

        [Export] private Label? _title;
        [Export] private Label? _empty;
        [Export] private Label? _planned;
        [Export] private Label? _conditional;

        [Export] private Label? _unlocksCaption;
        [Export] private Label? _parametersCaption;
        [Export] private Label? _knobsCaption;
        [Export] private Label? _keystonesCaption;

        [Export] private VBoxContainer? _unlocks;
        [Export] private VBoxContainer? _parameters;
        [Export] private VBoxContainer? _knobs;
        [Export] private VBoxContainer? _keystones;

        [Export] private PackedScene? _rowScene;

        private ModifierFormatter? _modifiers;
        private ContextModifierFormatter? _context;

        /// <summary>The two formatters the rest of the game words its numbers with. Optional, like
        /// everywhere else on this screen: a composition without them prints the raw parts rather than
        /// taking the panel down.</summary>
        public void UseFormatters(ModifierFormatter? modifiers, ContextModifierFormatter? context)
        {
            _modifiers = modifiers;
            _context = context;
        }

        /// <summary>
        /// Fills the panel from two readings of the same allocation: what the character HOLDS and what he
        /// WOULD hold once the plan is applied. The columns print the first, the delta column the
        /// difference — so the panel never quietly credits him with a plan he has not paid for.
        /// </summary>
        public void Show(TreeSummary held, TreeSummary projected, int plannedNodes)
        {
            _title?.Text = Localization.Localize(PassiveWheelText.SummaryTitle);
            ShowParameters(held, projected, plannedNodes > 0);
            ShowKnobs(held, projected, plannedNodes > 0);
            ShowUnlocks(held);
            ShowKeystones(held);

            if (_empty != null)
            {
                _empty.Visible = held.IsEmpty;
                if (_empty.Visible) _empty.Text = Localization.Localize(PassiveWheelText.SummaryEmpty);
            }

            if (_planned != null)
            {
                _planned.Visible = plannedNodes > 0;
                if (_planned.Visible)
                    _planned.Text = Localization.Render(PassiveWheelText.SummaryPlanned,
                        new Dictionary<string, object?> { [PassiveWheelText.CountValue] = plannedNodes });
            }

            if (_conditional == null) return;

            string note = Uncounted(held);
            _conditional.Visible = note.Length > 0;
            _conditional.Text = note;
        }

        /// <summary>What the totals above do NOT cover, one sentence per reason: lines held up by a gate,
        /// and lines whose value is per unit of a parameter this panel has no carrier to measure. Both share
        /// the one footnote the scene owns — a count that never reached the player would leave a taken node
        /// looking like it gave nothing at all.</summary>
        private static string Uncounted(TreeSummary held)
        {
            List<string> notes = [];
            if (held.ConditionalLines > 0) notes.Add(Counted(PassiveWheelText.SummaryConditional, held.ConditionalLines));
            if (held.ScaledLines > 0) notes.Add(Counted(PassiveWheelText.SummaryScaled, held.ScaledLines));

            return string.Join("\n", notes);
        }

        private static string Counted(string key, int lines) =>
            Localization.Render(key, new Dictionary<string, object?> { [PassiveWheelText.CountValue] = lines });

        public static PackedScene? Initialize() =>
            string.IsNullOrEmpty(UID) ? null : ResourceLoader.Load<PackedScene>(UID);

        private void ShowParameters(TreeSummary held, TreeSummary projected, bool planned)
        {
            if (_parameters == null) return;

            Dictionary<EntityParameter, ParameterTotal> ahead = [];
            foreach (ParameterTotal total in projected.Parameters) ahead[total.Parameter] = total;

            _parameters.QueueFreeChildren();
            List<ParameterTotal> rows = Merged(held.Parameters, projected.Parameters);
            if (rows.Count > 0) Header();

            foreach (ParameterTotal total in rows)
            {
                ParameterTotal? after = ahead.GetValueOrDefault(total.Parameter);
                Row(_parameters)?.Show(
                    Localization.Localize(total.Parameter.ToString()),
                    Bucket(total.Parameter, ModifierValueType.Flat, total.Flat),
                    Bucket(total.Parameter, ModifierValueType.Increase, total.Increase),
                    Bucket(total.Parameter, ModifierValueType.Multiplicative, total.Multiplicative),
                    Value(total.Parameter, ModifierValueType.Flat, total.Total),
                    planned ? Delta(total.Parameter, total.Total, after?.Total ?? 0f) : string.Empty);
            }

            Section(_parametersCaption, PassiveWheelText.SummaryParameters, _parameters.GetChildCount() > 0);
        }

        private void ShowKnobs(TreeSummary held, TreeSummary projected, bool planned)
        {
            if (_knobs == null) return;

            Dictionary<ContextParameter, float> ahead = [];
            foreach (ContextTotal total in projected.Context) ahead[total.Parameter] = total.Value;

            _knobs.QueueFreeChildren();
            foreach (ContextTotal total in MergedKnobs(held.Context, projected.Context))
            {
                var entry = new ContextModifierEntry(total.Parameter, total.Bucket ?? ModifierValueType.Increase, total.Value);
                string sentence = _context?.Format(entry) ?? $"{total.Parameter} {total.Value}";
                float after = ahead.GetValueOrDefault(total.Parameter);

                Row(_knobs)?.Show(sentence, string.Empty, string.Empty, string.Empty, string.Empty,
                    planned && !Mathf.IsEqualApprox(after, total.Value) ? Signed(after - total.Value) : string.Empty);
            }

            Section(_knobsCaption, PassiveWheelText.SummaryKnobs, _knobs.GetChildCount() > 0);
        }

        private void ShowUnlocks(TreeSummary held)
        {
            if (_unlocks == null) return;

            _unlocks.QueueFreeChildren();
            foreach (KeyValuePair<PassiveNodeKind, List<string>> group in held.Unlocks)
                foreach (string abilityId in group.Value)
                    Row(_unlocks)?.Show(Localization.Localize(abilityId), string.Empty, string.Empty,
                        string.Empty, Localization.Localize($"{PassiveWheelText.KindPrefix}{group.Key}"), string.Empty);

            Section(_unlocksCaption, PassiveWheelText.SummaryUnlocks, _unlocks.GetChildCount() > 0);
        }

        private void ShowKeystones(TreeSummary held)
        {
            if (_keystones == null) return;

            _keystones.QueueFreeChildren();
            foreach (string keystone in held.Keystones)
                Row(_keystones)?.Show(keystone, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

            Section(_keystonesCaption, PassiveWheelText.SummaryKeystones, _keystones.GetChildCount() > 0);
        }

        /// <summary>Every parameter either reading knows about, so a line the plan would ADD and one it
        /// would REMOVE are both on the table instead of only the ones that survive it.</summary>
        private static List<ParameterTotal> Merged(List<ParameterTotal> held, List<ParameterTotal> projected)
        {
            Dictionary<EntityParameter, ParameterTotal> rows = [];
            foreach (ParameterTotal total in held) rows[total.Parameter] = total;
            foreach (ParameterTotal total in projected)
                rows.TryAdd(total.Parameter, total with { Flat = 0f, Increase = 0f, Multiplicative = 0f, Total = 0f });

            List<ParameterTotal> merged = [.. rows.Values];
            merged.Sort(static (first, second) =>
                string.CompareOrdinal(first.Parameter.ToString(), second.Parameter.ToString()));
            return merged;
        }

        private static List<ContextTotal> MergedKnobs(List<ContextTotal> held, List<ContextTotal> projected)
        {
            Dictionary<ContextParameter, ContextTotal> rows = [];
            foreach (ContextTotal total in held) rows[total.Parameter] = total;
            foreach (ContextTotal total in projected) rows.TryAdd(total.Parameter, total with { Value = 0f });

            List<ContextTotal> merged = [.. rows.Values];
            merged.Sort(static (first, second) =>
                string.CompareOrdinal(first.Parameter.ToString(), second.Parameter.ToString()));
            return merged;
        }

        private static void Section(Label? caption, string key, bool visible)
        {
            if (caption == null) return;

            caption.Visible = visible;
            if (visible) caption.Text = Localization.Localize(key);
        }

        private static string Signed(float delta) =>
            delta >= 0f ? $"+{delta:0.#}" : $"{delta:0.#}";

        /// <summary>The column names, as a row of the same kind the numbers are printed in — so the four
        /// columns cannot drift apart from the words above them.</summary>
        private void Header()
        {
            if (_parameters == null) return;

            Row(_parameters)?.Show(
                string.Empty,
                Localization.Localize(PassiveWheelText.SummaryColumnFlat),
                Localization.Localize(PassiveWheelText.SummaryColumnIncrease),
                Localization.Localize(PassiveWheelText.SummaryColumnMore),
                Localization.Localize(PassiveWheelText.SummaryColumnTotal),
                string.Empty);
        }

        private PassiveSummaryRow? Row(VBoxContainer into)
        {
            if (_rowScene == null) return null;

            var row = _rowScene.Instantiate<PassiveSummaryRow>();
            into.AddChild(row);
            return row;
        }

        /// <summary>A bucket that summed to nothing is not printed at all.</summary>
        private string Bucket(EntityParameter parameter, ModifierValueType valueType, float value) =>
            Mathf.IsZeroApprox(value) ? string.Empty : Value(parameter, valueType, value);

        private string Value(EntityParameter parameter, ModifierValueType valueType, float value) =>
            _modifiers?.FormatValue(valueType, parameter, value) ?? $"{value:0.#}";

        private string Delta(EntityParameter parameter, float held, float projected) =>
            Mathf.IsEqualApprox(held, projected)
                ? string.Empty
                : (projected >= held ? "+" : "-") + Value(parameter, ModifierValueType.Flat, Mathf.Abs(projected - held));
    }
}
