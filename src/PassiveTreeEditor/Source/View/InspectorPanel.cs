namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Modifiers.Conditions;
    using Core.Modifiers.Context;
    using Core.PassiveTree;
    using Godot;
    using Io;

    /// <summary>
    /// Properties of the selected node. Rebuilt wholesale on every selection change — a node has a
    /// handful of controls, and rebuilding removes a whole class of stale-binding bugs that partial
    /// refreshes invite.
    /// </summary>
    public partial class InspectorPanel : VBoxContainer
    {
        /// <summary>What a line carrying a number may be. Flag is deliberately absent: it is a switch,
        /// and a switch belongs only to the context knobs whose binding reads no value at all.</summary>
        private static readonly ModifierValueType[] s_numericValueTypes =
        [
            ModifierValueType.Flat,
            ModifierValueType.Increase,
            ModifierValueType.Multiplicative
        ];

        private AbilityCatalog? _abilities;
        private ConditionProvider? _conditions;
        private TreeCanvas? _canvas;

        /// <summary>A cosmetic edit: the canvas needs a redraw, the totals do not.</summary>
        public event Action? NodeEdited;

        /// <summary>An edit that changes what the allocation is worth, or which nodes exist under
        /// which id. Kept separate so that typing a title does not rebuild the totals per keystroke.</summary>
        public event Action? TotalsChanged;

        public void Initialize(TreeCanvas canvas, AbilityCatalog abilities, ConditionProvider conditions)
        {
            _canvas = canvas;
            _abilities = abilities;
            _conditions = conditions;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 6);
        }

        public void Rebuild()
        {
            this.ClearContent();

            if (_canvas is null) return;

            if (_canvas.Selection.Count == 0)
            {
                AddChild(EditorControls.Wrapped("Nothing selected.\n\nSelect mode: click a node, drag to move, box-drag to\nmulti-select, Delete removes. Middle or right mouse\ndrag pans, wheel zooms, F frames the tree."));
                return;
            }

            if (_canvas.Selection.Count > 1)
            {
                AddChild(EditorControls.Wrapped($"{_canvas.Selection.Count} nodes selected.\nDrag moves them together; Delete removes them."));
                return;
            }

            PassiveNode? node = _canvas.SingleSelection;
            if (node is null) return;

            BuildIdentity(node);
            BuildText(node);
            BuildModifiers(node);
        }

        private void Changed()
        {
            _canvas?.QueueRedraw();
            NodeEdited?.Invoke();
        }

        private void ChangedTotals()
        {
            Changed();
            TotalsChanged?.Invoke();
        }

        private void BuildIdentity(PassiveNode node)
        {
            AddChild(EditorControls.Caption("NODE"));

            var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddChild(grid);

            grid.AddChild(new Label { Text = "Id" });
            var idEdit = new LineEdit { Text = node.Id, SizeFlagsHorizontal = SizeFlags.ExpandFill };

            // Committed on Enter *and* on leaving the field. Enter-only loses the edit when the mouse
            // moves on, and a rename that silently did not happen is invisible — the id keeps looking
            // renamed in a field that no longer owns it.
            void CommitId(string text)
            {
                if (_canvas is null) return;

                string wanted = text.Trim();
                if (wanted.Length == 0 || wanted == node.Id) return;

                if (_canvas.Document.Rename(node.Id, wanted))
                {
                    _canvas.SelectOnly(wanted);
                    ChangedTotals();
                }
                else
                {
                    idEdit.Text = node.Id;
                }
            }

            idEdit.TextSubmitted += CommitId;
            idEdit.FocusExited += () => CommitId(idEdit.Text);
            grid.AddChild(idEdit);

            grid.AddChild(new Label { Text = "Kind" });
            grid.AddChild(EditorControls.Picker(node.Kind, kind =>
            {
                node.Kind = kind;
                ChangedTotals();
                Rebuild();
            }));

            grid.AddChild(new Label { Text = "Stance" });
            grid.AddChild(EditorControls.OptionalPicker<Stance>(node.Stance, value =>
            {
                node.Stance = value;
                Changed();
                Rebuild();
            }));

            // The second ray only means something once there is a first one to bridge from.
            if (node.Stance is not null)
            {
                grid.AddChild(new Label { Text = "Hybrid" });
                grid.AddChild(EditorControls.OptionalPicker<Stance>(node.HybridStance, value =>
                {
                    node.HybridStance = value;
                    Changed();
                    Rebuild();
                }));
            }

            grid.AddChild(new Label { Text = "X" });
            grid.AddChild(PositionBox(node, horizontal: true));
            grid.AddChild(new Label { Text = "Y" });
            grid.AddChild(PositionBox(node, horizontal: false));

            grid.AddChild(new Label { Text = "Title" });
            var titleEdit = new LineEdit { Text = node.Title, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            titleEdit.TextChanged += text =>
            {
                node.Title = text;
                Changed();
            };
            grid.AddChild(titleEdit);

            if (!NodeKindRules.For(node.Kind).RequiresAbility) return;

            grid.AddChild(new Label { Text = "Ability" });
            grid.AddChild(AbilityPicker(node));
        }

        private SpinBox PositionBox(PassiveNode node, bool horizontal)
        {
            SpinBox box = EditorControls.Number(horizontal ? node.X : node.Y, 1);

            box.ValueChanged += value =>
            {
                if (horizontal) node.X = (float)value;
                else node.Y = (float)value;

                _canvas?.Document.Reindex();
                Changed();
            };

            return box;
        }

        private OptionButton AbilityPicker(PassiveNode node) =>
            EditorControls.IdPicker(AbilityIds(), node.AbilityId, id =>
            {
                node.AbilityId = id;
                ChangedTotals();
            });

        private List<string> AbilityIds()
        {
            List<string> ids = [];
            if (_abilities is null) return ids;

            foreach (AbilityEntry entry in _abilities.Selectable()) ids.Add(entry.Id);
            return ids;
        }

        private void BuildText(PassiveNode node)
        {
            bool isRule = NodeKindRules.For(node.Kind).UsesRuleText;
            AddChild(EditorControls.Caption(isRule ? "RULE TEXT" : "NOTES"));

            var text = new TextEdit
            {
                Text = node.Description,
                CustomMinimumSize = new Vector2(0, isRule ? 110 : 60),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                WrapMode = TextEdit.LineWrappingMode.Boundary
            };

            text.TextChanged += () =>
            {
                node.Description = text.Text;
                Changed();
            };

            AddChild(text);
        }

        private void BuildModifiers(PassiveNode node)
        {
            NodeKindRule rule = NodeKindRules.For(node);
            AddChild(EditorControls.Caption($"MODIFIERS  {node.LineCount}/{rule.MaxModifiers}"
                                           + (node.IsHybrid ? "   hybrid" : string.Empty)));

            if (rule.MaxModifiers == 0 && node.LineCount == 0)
            {
                AddChild(EditorControls.Wrapped("This node class carries no modifier lines."));
                return;
            }

            // Lines left over from a kind change are still shown so they can be removed by hand. The
            // tool never silently drops authored content — Check reports the excess instead.
            for (int index = 0; index < node.Modifiers.Count; index++) AddChild(ModifierRow(node, index));
            for (int index = 0; index < node.ContextModifiers.Count; index++) AddChild(ContextRow(node, index));

            var buttons = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            buttons.AddChild(AddButton("+ line", node, rule, () => node.Modifiers.Add(new ModifierLine())));
            buttons.AddChild(AddButton("+ context", node, rule, () => node.ContextModifiers.Add(new ContextModifierLine())));
            AddChild(buttons);
        }

        /// <summary>Both channels share the node's line budget, so both buttons close at the same count.</summary>
        private Button AddButton(string text, PassiveNode node, NodeKindRule rule, Action add)
        {
            var button = new Button
            {
                Text = text,
                Disabled = node.LineCount >= rule.MaxModifiers,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            button.Pressed += () =>
            {
                add();
                ChangedTotals();
                Rebuild();
            };

            return button;
        }

        private Control ModifierRow(PassiveNode node, int index)
        {
            ModifierLine line = node.Modifiers[index];

            var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var top = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            box.AddChild(top);

            top.AddChild(EditorControls.Picker(line.Parameter, parameter =>
            {
                line.Parameter = parameter;
                ChangedTotals();
            }));
            top.AddChild(RemoveButton(() => node.Modifiers.RemoveAt(index)));

            var bottom = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            box.AddChild(bottom);

            // The hint label comes first: the value-type callback writes into it, so it has to exist
            // before the picker that captures it is built.
            var hint = new Label { CustomMinimumSize = new Vector2(64, 0) };

            OptionButton typePicker = EditorControls.Picker(s_numericValueTypes, line.ValueType, type =>
            {
                line.ValueType = type;
                hint.Text = ValueHint(line.ValueType, line.Value);
                ChangedTotals();
            });

            // The value box owns the free width of the row: the number is what gets typed, the type
            // is picked once.
            typePicker.SizeFlagsHorizontal = SizeFlags.Fill;
            bottom.AddChild(typePicker);

            SpinBox value = EditorControls.Number(line.Value, 0.001);
            bottom.AddChild(value);
            bottom.AddChild(hint);

            value.ValueChanged += amount =>
            {
                line.Value = (float)amount;
                hint.Text = ValueHint(line.ValueType, line.Value);
                ChangedTotals();
            };

            hint.Text = ValueHint(line.ValueType, line.Value);
            box.AddChild(ConditionPicker(line.Condition, id => line.Condition = id));

            return box;
        }

        /// <summary>A context line: same shape as a parametric one, aimed at a pipeline knob instead of
        /// a parameter. Flag belongs to this row only — a switch has no place in parameter math — and
        /// within the row only to the knobs that are switches.</summary>
        private Control ContextRow(PassiveNode node, int index)
        {
            ContextModifierLine line = node.ContextModifiers[index];

            var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var top = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            box.AddChild(top);

            // Only wired knobs are offered. A line naming a knob no pipeline reads is refused when the
            // file is loaded, so the tool must not be able to author one in the first place.
            top.AddChild(EditorControls.Picker(ContextKnobs.Bound, line.Parameter, parameter =>
            {
                line.Parameter = parameter;

                // The knob owns which kind of line it takes, so swapping it re-picks a type the new knob
                // accepts. The authored number is left untouched: it means nothing while the knob is a
                // switch, and it is still there when a knob that reads a value comes back. The type is not
                // carried the same way — a line written as Flat or Multiplicative returns as Increase.
                // Saving while the knob is a switch is what ends the number's life; see the value box.
                if (ContextKnobs.WhyRefused(parameter, line.ValueType) is not null)
                    line.ValueType = ContextKnobs.IsFlag(parameter) ? ModifierValueType.Flag : ModifierValueType.Increase;

                ChangedTotals();
                Rebuild();
            }));
            top.AddChild(RemoveButton(() => node.ContextModifiers.RemoveAt(index)));

            var bottom = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            box.AddChild(bottom);

            var hint = new Label { CustomMinimumSize = new Vector2(64, 0) };
            SpinBox value = EditorControls.Number(line.Value, 0.001);

            OptionButton typePicker = EditorControls.Picker(ContextValueTypes(line.Parameter), line.ValueType, type =>
            {
                line.ValueType = type;
                hint.Text = ValueHint(line.ValueType, line.Value);
                ChangedTotals();
            });

            typePicker.SizeFlagsHorizontal = SizeFlags.Fill;
            bottom.AddChild(typePicker);
            bottom.AddChild(value);
            bottom.AddChild(hint);

            // A switch carries no number: the box shows the pinned value and neither takes input nor
            // writes back, so the number a numeric knob left behind survives a detour through a switch
            // and back — in this row, for as long as it lives. It does not survive the file: a flag line
            // is written with no value at all, so a save while the knob is a switch loses the number and
            // a reload brings the row back at zero.
            value.Editable = !line.IsFlag;
            value.ValueChanged += amount =>
            {
                if (line.IsFlag) return;

                line.Value = (float)amount;
                hint.Text = ValueHint(line.ValueType, line.Value);
                ChangedTotals();
            };

            hint.Text = ValueHint(line.ValueType, line.Value);
            box.AddChild(ConditionPicker(line.Condition, id => line.Condition = id));

            return box;
        }

        private Button RemoveButton(Action remove)
        {
            var button = new Button { Text = "×" };

            button.Pressed += () =>
            {
                remove();
                ChangedTotals();
                Rebuild();
            };

            return button;
        }

        /// <summary>The condition field of either line channel, offering the catalog and nothing else.
        /// A condition is a reference: the game resolves the id when it builds the contribution and drops
        /// the whole line when the catalog has no such entry, so an id that could be typed would be a way
        /// to author a line that quietly never applies.</summary>
        private OptionButton ConditionPicker(string current, Action<string> apply) =>
            EditorControls.IdPicker(_conditions?.Ids ?? [], current, id =>
            {
                apply(id);
                ChangedTotals();
            }, emptyLabel: "— always");

        /// <summary>What a context line on this knob may be. A switch takes nothing but Flag, and a knob
        /// that reads a value never takes it — both readers refuse the other half, so the tool must not
        /// be able to offer it.</summary>
        private static IReadOnlyList<ModifierValueType> ContextValueTypes(ContextParameter parameter) =>
            ContextKnobs.IsFlag(parameter) ? [ModifierValueType.Flag] : s_numericValueTypes;

        private static string ValueHint(ModifierValueType valueType, float value) => valueType switch
        {
            ModifierValueType.Flag => "switch",
            ModifierValueType.Flat => string.Empty,
            _ => $"= {EditorControls.Percent(value)}"
        };
    }
}
