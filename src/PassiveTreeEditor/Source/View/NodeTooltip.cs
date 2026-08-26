namespace PassiveTreeEditor.Source.View
{
    using System.Collections.Generic;
    using System.Globalization;
    using Core.Battle.Skills;
    using Core.Localization;
    using Core.Modifiers;
    using Core.PassiveTree;
    using Core.Views.UI;
    using Godot;

    /// <summary>
    /// What a node gives, shown at the cursor. Modifier lines go through the game's own
    /// <see cref="ModifierFormatter"/>, so a line reads here exactly as it will read in the item
    /// tooltip — same .po templates, same percent-versus-number decision per parameter.
    /// </summary>
    public partial class NodeTooltip : PanelContainer
    {
        private const float CursorOffset = 18f;
        private const float Width = 280f;

        private VBoxContainer _content = null!;
        private ModifierFormatter? _formatter;
        private ContextModifierFormatter? _contextFormatter;
        private ILocalizationProvider? _localization;

        public override void _Ready()
        {
            // Never a mouse target: the tooltip follows the cursor, and eating hover would make the
            // node under it stop being hovered — the tooltip would flicker itself out of existence.
            MouseFilter = MouseFilterEnum.Ignore;
            Visible = false;
            CustomMinimumSize = new Vector2(Width, 0);
            ZIndex = 100;

            AddThemeStyleboxOverride("panel", BuildStyle());

            _content = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            _content.AddThemeConstantOverride("separation", 4);
            AddChild(_content);
        }

        public void Initialize(ModifierFormatter formatter, ContextModifierFormatter contextFormatter, ILocalizationProvider localization)
        {
            _formatter = formatter;
            _contextFormatter = contextFormatter;
            _localization = localization;
        }

        private static StyleBoxFlat BuildStyle()
        {
            var style = new StyleBoxFlat
            {
                BgColor = CanvasStyle.TooltipBackground,
                BorderColor = CanvasStyle.GoldInk,
                ContentMarginLeft = 12,
                ContentMarginRight = 12,
                ContentMarginTop = 10,
                ContentMarginBottom = 10
            };

            style.SetBorderWidthAll(1);
            return style;
        }

        public void HideTip() => Visible = false;

        /// <summary>Fills in the node and places the card next to the cursor, kept inside the viewport
        /// so it never hangs off the window edge.</summary>
        public void ShowFor(PassiveNode node, Vector2 mouseGlobal)
        {
            Build(node);

            Visible = true;

            // Children were added this frame and layout has not run, so Size is still what the previous
            // node needed — PlaceClamped reads Size, so it has to be set first.
            Size = GetCombinedMinimumSize();
            UiPlacement.PlaceClamped(this, mouseGlobal, new Vector2(CursorOffset, CursorOffset));
        }

        private void Build(PassiveNode node)
        {
            _content.ClearContent();

            _content.AddChild(Header(node));

            if (!string.IsNullOrWhiteSpace(node.AbilityId))
                _content.AddChild(Row(Translate(node.AbilityId), CanvasStyle.Ink1));

            if (node.IsPassive)
                foreach (string line in PassiveLines(node)) _content.AddChild(Row(line, CanvasStyle.Ink2));

            foreach (ModifierLine line in node.Modifiers) _content.AddChild(Row(Describe(line), CanvasStyle.Ink2));
            foreach (ContextModifierLine line in node.ContextModifiers) _content.AddChild(Row(Describe(line), CanvasStyle.Ink2));

            if (!string.IsNullOrWhiteSpace(node.Description))
                _content.AddChild(Row(node.Description, CanvasStyle.Ink3));
        }

        private Label Header(PassiveNode node)
        {
            var label = Row(CanvasStyle.ShortLabel(node), CanvasStyle.NodeColor(node));
            label.AddThemeFontSizeOverride("font_size", 15);
            return label;
        }

        private static Label Row(string text, Color color)
        {
            var label = new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(Width - 24f, 0),
                MouseFilter = MouseFilterEnum.Ignore
            };

            label.AddThemeColorOverride("font_color", color);
            return label;
        }

        /// <summary>A conditional line is shown as written and marked: the tool has no battle state to
        /// decide whether the condition holds, so it never hides the condition behind a number.</summary>
        private string Describe(ModifierLine line)
        {
            string text = _formatter is null
                ? $"{line.Parameter} {line.ValueType} {line.Value}{(line.IsScaled ? $" per {line.PerParameter}" : string.Empty)}"
                : _formatter.Format(
                    new SimpleModifier(line.Parameter, line.ValueType, line.Value, PassiveTreeDocument.ModifierSource), TextFormat.Plain, line.PerParameter);

            return line.IsConditional ? $"{text}  ({line.Condition})" : text;
        }

        /// <summary>The context line reads through the game's own knob templates. The entry handed to the
        /// formatter exists for the sentence and nothing else — it is never attached to anyone, because
        /// what a fighter gets is the sum of the taken lines, not one modifier per node.</summary>
        private string Describe(ContextModifierLine line)
        {
            string text = _contextFormatter is null
                ? $"{line.Parameter} {line.ValueType} {line.Value}"
                : _contextFormatter.Format(new ContextModifierEntry(line.Parameter, line.ValueType, line.Value));

            return line.IsConditional ? $"{text}  ({line.Condition})" : text;
        }

        /// <summary>
        /// What the passive on the node says. A stat-family id is nothing but its fields, so they are read
        /// through the game's own grammar and worded by the same formatter a modifier line goes through —
        /// the card and the wheel say the same sentence. All of them or none: one unreadable field refuses
        /// the whole grant, and a card printing the readable half would promise lines the game withholds.
        /// <para>Everything else is a factory living in the game, out of this tool's reach: the id and the
        /// numbers it is tuned by are the whole of what can honestly be shown.</para>
        /// </summary>
        private List<string> PassiveLines(PassiveNode node)
        {
            List<StatPassiveLine> stats = StatPassiveGrammar.Owns(node.PassiveId)
                ? StatPassiveGrammar.ReadWhole(node.Properties)
                : [];

            if (stats.Count > 0) return stats.ConvertAll(Describe);

            List<string> lines = [node.PassiveId ?? string.Empty];
            foreach (KeyValuePair<string, float> property in node.Properties)
                lines.Add($"{property.Key} = {property.Value.ToString("0.###", CultureInfo.InvariantCulture)}");

            return lines;
        }

        /// <summary>Worded by the game's own reading of a stat line rather than by a second one here: a card
        /// spelling the same record differently from the wheel is the drift this tool exists to prevent.</summary>
        private string Describe(StatPassiveLine line) => StatPassiveLineText.Line(line, _formatter);

        private string Translate(string key) => _localization?.Translate(key) ?? key;
    }
}
