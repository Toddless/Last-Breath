namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Modifiers.Conditions;
    using Core.Modifiers.Context;
    using Core.PassiveTree;
    using Editing;
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

        /// <summary>Survives the rebuilds, because the rows do not: a row is thrown away the moment its
        /// knob changes, and what the author typed has to outlive the control that took it.</summary>
        private readonly ContextLineValues _contextValues = new();

        private AbilityCatalog? _abilities;
        private ConditionProvider? _conditions;
        private TreeCanvas? _canvas;

        /// <summary>Every field on this panel changes the tree through here. A control that wrote into
        /// a node directly would be a change the undo stack never heard of.</summary>
        private TreeEditor _editor = null!;

        /// <summary>A cosmetic edit: the canvas needs a redraw, the totals do not.</summary>
        public event Action? NodeEdited;

        /// <summary>An edit that changes what the allocation is worth, or which nodes exist under
        /// which id. Kept separate so that typing a title does not rebuild the totals per keystroke.</summary>
        public event Action? TotalsChanged;

        public void Initialize(TreeCanvas canvas, TreeEditor editor, AbilityCatalog abilities, ConditionProvider conditions)
        {
            _canvas = canvas;
            _editor = editor;
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

        /// <summary>
        /// Ends the run of edits a control was taking when the author leaves it, so that coming back to
        /// the same box later is a second step and one undo takes back one of them. Every control that
        /// can be edited more than once in a row passes through here — the run is what merges edits into
        /// one step, and a control left out would merge two separate visits to it into one.
        /// <para>A spin box keeps its number in a line edit of its own, and that is where the focus
        /// goes; asking the box itself would be asking a control that never had it.</para>
        /// </summary>
        private SpinBox Sealing(SpinBox box)
        {
            box.GetLineEdit().FocusExited += _editor.Seal;
            return box;
        }

        private OptionButton Sealing(OptionButton picker)
        {
            picker.FocusExited += _editor.Seal;
            return picker;
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

            // The id this field was built to describe. It is not always the node's id any more by the
            // time the field writes back — see the staleness guard below.
            string shown = node.Id;

            // Committed on Enter *and* on leaving the field. Enter-only loses the edit when the mouse
            // moves on, and a rename that silently did not happen is invisible — the id keeps looking
            // renamed in a field that no longer owns it.
            void CommitId(string text)
            {
                // A field can outlive the state it was built for. A step back through the history
                // renames the node and rebuilds this panel, and a control being torn down reports the
                // focus it is losing — so this runs one last time, holding the text from before the
                // undo. Writing it would put the undone rename straight back, which is the one way an
                // undo can leave the author with a tree they did not ask for and did not notice.
                if (_canvas is null || !string.Equals(node.Id, shown, StringComparison.Ordinal)) return;

                string wanted = text.Trim();
                if (wanted.Length == 0 || wanted == node.Id) return;

                if (_editor.Rename(node, wanted))
                {
                    shown = wanted;
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
            grid.AddChild(Sealing(EditorControls.Picker(node.Kind, kind =>
            {
                _editor.SetNodeValue(node, EditFields.Kind, node.Kind, kind, value => node.Kind = value);
                ChangedTotals();
                Rebuild();
            })));

            grid.AddChild(new Label { Text = "Stance" });
            grid.AddChild(Sealing(EditorControls.OptionalPicker<Stance>(node.Stance, value =>
            {
                _editor.SetNodeValue(node, EditFields.Stance, node.Stance, value, stance => node.Stance = stance);
                Changed();
                Rebuild();
            })));

            // The second ray only means something once there is a first one to bridge from.
            if (node.Stance is not null)
            {
                grid.AddChild(new Label { Text = "Hybrid" });
                grid.AddChild(Sealing(EditorControls.OptionalPicker<Stance>(node.HybridStance, value =>
                {
                    _editor.SetNodeValue(node, EditFields.Hybrid, node.HybridStance, value, stance => node.HybridStance = stance);
                    Changed();
                    Rebuild();
                })));
            }

            grid.AddChild(new Label { Text = "X" });
            grid.AddChild(PositionBox(node, horizontal: true));
            grid.AddChild(new Label { Text = "Y" });
            grid.AddChild(PositionBox(node, horizontal: false));

            grid.AddChild(new Label { Text = "Title" });
            var titleEdit = new LineEdit { Text = node.Title, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            titleEdit.TextChanged += text =>
            {
                _editor.SetNodeValue(node, EditFields.Title, node.Title, text, value => node.Title = value);
                Changed();
            };

            // Leaving the field ends the run of keystrokes: coming back to it later is a second edit,
            // and one undo has to take back one of them rather than both.
            titleEdit.FocusExited += _editor.Seal;
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
                // Moving a node is moving it, whether the hand did it with a drag or with this box —
                // both put back the coordinate the node had, not an offset applied in reverse.
                _editor.SetNodeValue(node,
                    horizontal ? EditFields.X : EditFields.Y,
                    horizontal ? node.X : node.Y,
                    (float)value,
                    Place(node, horizontal));

                Changed();
            };

            return Sealing(box);
        }

        /// <summary>Writing a coordinate moves geometry the graph itself knows nothing about, so the
        /// spatial grid has to be told. It sits inside the action rather than at the call site because
        /// the history replays this same action on the way back.</summary>
        private Action<float> Place(PassiveNode node, bool horizontal) => value =>
        {
            if (horizontal) node.X = value;
            else node.Y = value;

            _editor.Document.Reindex();
        };

        private OptionButton AbilityPicker(PassiveNode node) =>
            Sealing(EditorControls.IdPicker(AbilityIds(), node.AbilityId, id =>
            {
                _editor.SetNodeValue(node, EditFields.Ability, node.AbilityId, id, value => node.AbilityId = value);
                ChangedTotals();
            }));

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
                _editor.SetNodeValue(node, EditFields.Description, node.Description, text.Text,
                    value => node.Description = value);

                Changed();
            };

            text.FocusExited += _editor.Seal;
            AddChild(text);
        }

        private void BuildModifiers(PassiveNode node)
        {
            NodeKindRule rule = NodeKindRules.For(node.Kind);
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
            buttons.AddChild(AddButton("+ line", node, rule, () => _editor.AddModifierLine(node)));
            buttons.AddChild(AddButton("+ context", node, rule, () => _editor.AddContextLine(node)));
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

            top.AddChild(Sealing(EditorControls.Picker(line.Parameter, parameter =>
            {
                _editor.EditLine(node, line, EditFields.Parameter, () => line.Parameter = parameter);
                ChangedTotals();
            })));
            top.AddChild(RemoveButton(() => _editor.RemoveModifierLine(node, index)));

            var bottom = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            box.AddChild(bottom);

            // The hint label comes first: the value-type callback writes into it, so it has to exist
            // before the picker that captures it is built.
            var hint = new Label { CustomMinimumSize = new Vector2(64, 0) };

            OptionButton typePicker = Sealing(EditorControls.Picker(s_numericValueTypes, line.ValueType, type =>
            {
                _editor.EditLine(node, line, EditFields.ValueType, () => line.ValueType = type);
                hint.Text = ValueHint(line.ValueType, line.Value);
                ChangedTotals();
            }));

            // The value box owns the free width of the row: the number is what gets typed, the type
            // is picked once.
            typePicker.SizeFlagsHorizontal = SizeFlags.Fill;
            bottom.AddChild(typePicker);

            SpinBox value = Sealing(EditorControls.Number(line.Value, 0.001));
            bottom.AddChild(value);
            bottom.AddChild(hint);

            value.ValueChanged += amount =>
            {
                _editor.EditLine(node, line, EditFields.Value, () => line.Value = (float)amount);
                hint.Text = ValueHint(line.ValueType, line.Value);
                ChangedTotals();
            };

            hint.Text = ValueHint(line.ValueType, line.Value);
            box.AddChild(ConditionPicker(line.Condition,
                id => _editor.EditLine(node, line, EditFields.Condition, () => line.Condition = id)));

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
            top.AddChild(Sealing(EditorControls.Picker(ContextKnobs.Bound, line.Parameter, parameter =>
            {
                // Which kind of line the new knob makes of this one is the plain class's rule, not the
                // control's: pointing a line at a switch turns it into one, and coming back to a knob
                // that reads a value hands the authored number and its bucket back, so a detour through
                // a switch costs the author nothing. Saving while the knob is a switch is still what
                // ends the number's life — the file writes a flag line with no value at all.
                // One gesture, three fields: the history snapshots the whole line either side of it
                // rather than the knob alone.
                _editor.EditLine(node, line, EditFields.Knob, () => _contextValues.Retarget(line, parameter));

                ChangedTotals();
                Rebuild();
            })));
            top.AddChild(RemoveButton(() => _editor.RemoveContextLine(node, index)));

            var bottom = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            box.AddChild(bottom);

            var hint = new Label { CustomMinimumSize = new Vector2(64, 0) };
            SpinBox value = Sealing(EditorControls.Number(line.Value, 0.001));

            OptionButton typePicker = Sealing(EditorControls.Picker(ContextValueTypes(line.Parameter), line.ValueType, type =>
            {
                _editor.EditLine(node, line, EditFields.ValueType, () => line.ValueType = type);
                hint.Text = ValueHint(line.ValueType, line.Value);
                ChangedTotals();
            }));

            typePicker.SizeFlagsHorizontal = SizeFlags.Fill;
            bottom.AddChild(typePicker);
            bottom.AddChild(value);
            bottom.AddChild(hint);

            // A switch carries no number: the box shows the pinned value and neither takes input nor
            // writes back. The gate is decided once, when the row is built, and never re-read off the
            // line: a row outlives its own knob for as long as the rebuild that replaces it takes, and
            // a box standing in for a switch must not be able to author the pinned one as a quantity
            // whatever the line has turned into by the time the box is torn down.
            bool takesNumber = !line.IsFlag;

            value.Editable = takesNumber;
            value.ValueChanged += amount =>
            {
                if (!takesNumber) return;

                _editor.EditLine(node, line, EditFields.Value, () => line.Value = (float)amount);
                hint.Text = ValueHint(line.ValueType, line.Value);
                ChangedTotals();
            };

            hint.Text = ValueHint(line.ValueType, line.Value);
            box.AddChild(ConditionPicker(line.Condition,
                id => _editor.EditLine(node, line, EditFields.Condition, () => line.Condition = id)));

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
            Sealing(EditorControls.IdPicker(_conditions?.Ids ?? [], current, id =>
            {
                apply(id);
                ChangedTotals();
            }, emptyLabel: "— always"));

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
