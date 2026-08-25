namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using Core.Battle.Skills;
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
        /// <summary>Name a fresh property row is created under — never one already on the node.</summary>
        private const string NewFieldName = "field";

        private const string LinesHoldTheNode =
            "this node speaks in modifier lines — a node grants a passive or carries lines, never both";

        private const string PassiveHoldsTheNode =
            "this node grants a passive — a node grants a passive or carries lines, never both";

        private const string PassiveIdHint =
            "id of the passive this node hands over. " + StatPassiveGrammar.IdPrefix
            + "<Name> is built from the stat fields below and needs no code";

        private const string FieldNameHint =
            "field name the passive's factory reads. For the stat family: Parameter:ValueType"
            + " or Parameter:ValueType:PerParameter";

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

        /// <summary>The node's named numbers as the panel is showing them, in author order. Every gesture
        /// edits this list and hands the whole of it back, so the dictionary is rebuilt from a known order
        /// rather than patched key by key — which is what would let a renamed field jump position.</summary>
        private readonly List<KeyValuePair<string, float>> _properties = [];

        private AbilityCatalog? _abilities;
        private ConditionProvider? _conditions;
        private PassiveSkillCatalog? _passives;
        private TreeCanvas? _canvas;

        /// <summary>Every field on this panel changes the tree through here. A control that wrote into
        /// a node directly would be a change the undo stack never heard of.</summary>
        private TreeEditor _editor = null!;

        /// <summary>A cosmetic edit: the canvas needs a redraw, the totals do not.</summary>
        public event Action? NodeEdited;

        /// <summary>An edit that changes what the allocation is worth, or which nodes exist under
        /// which id. Kept separate so that typing a title does not rebuild the totals per keystroke.</summary>
        public event Action? TotalsChanged;

        public void Initialize(TreeCanvas canvas, TreeEditor editor, AbilityCatalog abilities,
            ConditionProvider conditions, PassiveSkillCatalog passives)
        {
            _canvas = canvas;
            _editor = editor;
            _abilities = abilities;
            _conditions = conditions;
            _passives = passives;
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

            // The panel's copy of the node's numbers, taken once per build: every property control edits
            // this list and hands the whole of it to the editor.
            _properties.Clear();
            _properties.AddRange(node.PropertyRows());

            BuildIdentity(node);
            BuildText(node);
            BuildPassive(node);
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

        // ── passive ────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// The passive the node hands over, and the numbers it is built from. A node reaches the fighter
        /// one way or the other, so naming a passive is shut off while the node carries lines and the line
        /// buttons are shut off while it names one — the reader drops a node written both ways whole, and
        /// the tool must not be able to author one at all.
        /// </summary>
        private void BuildPassive(PassiveNode node)
        {
            AddChild(EditorControls.Caption("PASSIVE"));

            var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            AddChild(grid);

            grid.AddChild(new Label { Text = "Id" });
            grid.AddChild(PassiveIdEdit(node));

            grid.AddChild(new Label { Text = "Known" });
            grid.AddChild(PassivePicker(node));

            if (PassiveWarning(node) is { } warning) AddChild(Warned(warning));

            BuildProperties(node);
        }

        /// <summary>The id itself, typed rather than picked: the stat family is any name under its prefix,
        /// and a tool that only offered a list would make a fresh keystone impossible to author.</summary>
        private LineEdit PassiveIdEdit(PassiveNode node)
        {
            bool blocked = node.HasLines;

            var idEdit = new LineEdit
            {
                Text = node.PassiveId ?? string.Empty,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Editable = !blocked,
                PlaceholderText = blocked ? "carries lines" : StatPassiveGrammar.IdPrefix,
                TooltipText = blocked ? LinesHoldTheNode : PassiveIdHint
            };

            idEdit.TextSubmitted += text => CommitPassiveId(node, idEdit, text);
            idEdit.FocusExited += () => CommitPassiveId(node, idEdit, idEdit.Text);

            // Leaving the field ends the run it was taking: coming back to it later is a second edit, and
            // one step back has to take back one of them.
            idEdit.FocusExited += _editor.Seal;
            return idEdit;
        }

        /// <summary>The catalog as a shortcut into the field beside it. Picking writes the id and nothing
        /// else — what the node grants stays a name the author can read and correct.</summary>
        private OptionButton PassivePicker(PassiveNode node)
        {
            OptionButton picker = Sealing(EditorControls.IdPicker(_passives?.Ids ?? [], node.PassiveId ?? string.Empty,
                id => SetPassiveId(node, id), emptyLabel: "— none"));

            picker.Disabled = node.HasLines;
            picker.TooltipText = node.HasLines ? LinesHoldTheNode : "passives a factory answers to";
            return picker;
        }

        /// <summary>Writes the id through the door that knows the channel rule, and rebuilds: what the
        /// property section asks for and whether the line buttons open both follow from it.</summary>
        private void SetPassiveId(PassiveNode node, string id)
        {
            if (!_editor.SetPassiveId(node, id)) return;

            ChangedTotals();
            Rebuild();
        }

        private void CommitPassiveId(PassiveNode node, LineEdit idEdit, string text)
        {
            // A field outlives the state it was built for: a step back through the history rewrites the
            // node and rebuilds this panel, and the control being torn down reports the focus it is
            // losing — writing the text it still holds would put the undone edit straight back.
            if (_canvas is null || string.Equals(text.Trim(), node.PassiveId ?? string.Empty, StringComparison.Ordinal)) return;

            if (_editor.SetPassiveId(node, text))
            {
                ChangedTotals();
                RebuildLater();
                return;
            }

            idEdit.Text = node.PassiveId ?? string.Empty;
        }

        /// <summary>Rebuilds once the gesture that asked for it is over. A field commits as it loses focus,
        /// and it loses focus while the panel is being torn down — rebuilding from inside that would leave
        /// two rebuilds walking the same children.</summary>
        private void RebuildLater() => Callable.From(Rebuild).CallDeferred();

        /// <summary>What is wrong with the passive as it stands, or null. Never a refusal — a tree is
        /// unfinished nearly all of the time — but an id no factory answers to is a node that will hand
        /// the player nothing, and it is cheaper to read here than in the game's log.</summary>
        private string? PassiveWarning(PassiveNode node)
        {
            // Stranded numbers are asked about whatever else the node is doing: they outlive the passive
            // that was named and then cleared, and a node carrying lines is exactly where nobody looks.
            if (PassiveNode.WhyChannelsCollide(node.PassiveId, node.HasLines) is { } collision) return collision;
            if (PassiveNode.WhyPropertiesAreStranded(node.PassiveId, node.Properties.Count) is { } stranded) return stranded;

            if (!node.IsPassive) return null;

            // The stat family answers for itself: it is built from its fields rather than from a factory,
            // so no catalog knows it and its fields are the only thing there is to check.
            if (StatPassiveGrammar.Owns(node.PassiveId)) return StatFieldWarning(node);

            if (_passives is null || _passives.IsEmpty) return null;

            if (!_passives.Knows(node.PassiveId))
                return $"'{node.PassiveId}' is in no passive catalog the tool read — the grant would be refused";

            return MissingFields(node) is { Count: > 0 } missing
                ? $"missing field(s): {string.Join(", ", missing)}"
                : null;
        }

        /// <summary>What the stat family cannot read. All-or-nothing, the way the grant is: a record with
        /// no fields is nothing at all, and one unreadable key refuses every line beside it.</summary>
        private static string? StatFieldWarning(PassiveNode node)
        {
            if (node.Properties.Count == 0) return "no stat fields — a stat passive is nothing but its lines";

            foreach (KeyValuePair<string, float> field in node.Properties)
                if (!StatPassiveGrammar.TryReadLine(field.Key, field.Value, out _, out string? refusal))
                    return $"field '{field.Key}' unreadable: {refusal}";

            return null;
        }

        /// <summary>Fields the named passive's factory reads that the node does not carry. The stat family
        /// answers with nothing: its fields are stat lines the author writes freely.</summary>
        private List<string> MissingFields(PassiveNode node)
        {
            List<string> missing = [];
            if (_passives is null) return missing;

            foreach (string field in _passives.RequiredFields(node.PassiveId))
                if (!node.Properties.ContainsKey(field))
                    missing.Add(field);

            return missing;
        }

        /// <summary>
        /// The named numbers, one row each, in the order the file keeps them. Every gesture rewrites the
        /// whole list rather than one key: a dictionary hands a fresh key the slot a removed one left
        /// behind, so a field added after a removal would otherwise land where the removed one stood.
        /// </summary>
        private void BuildProperties(PassiveNode node)
        {
            AddChild(EditorControls.Caption($"PROPERTIES  {_properties.Count}"));

            if (_properties.Count == 0 && !node.IsPassive)
            {
                AddChild(EditorControls.Wrapped("Name a passive above; the numbers it is tuned by go here."));
                return;
            }

            for (int index = 0; index < _properties.Count; index++) AddChild(PropertyRow(node, index));

            var add = new Button { Text = "+ field", SizeFlagsHorizontal = SizeFlags.ExpandFill };
            add.Pressed += () =>
            {
                _properties.Add(new KeyValuePair<string, float>(FreshFieldName(), 0f));
                ApplyList(node);
                ChangedTotals();
                Rebuild();
            };

            AddChild(add);
        }

        private Control PropertyRow(PassiveNode node, int index)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            var name = new LineEdit
            {
                Text = _properties[index].Key,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(150, 0),
                TooltipText = FieldNameHint
            };

            name.TextSubmitted += text => CommitFieldName(node, name, index, text);
            name.FocusExited += () => CommitFieldName(node, name, index, name.Text);
            name.FocusExited += _editor.Seal;
            row.AddChild(name);

            SpinBox value = Sealing(EditorControls.Number(_properties[index].Value, 0.001));
            value.ValueChanged += amount =>
            {
                if (index >= _properties.Count) return;

                _properties[index] = new KeyValuePair<string, float>(_properties[index].Key, (float)amount);

                // Each row merges on its own identity, so stepping one number and then another is two
                // steps back rather than one.
                ApplyProperties(node, $"{EditFields.PropertyValue} {index}");
            };

            row.AddChild(value);
            // The button tells the totals and rebuilds for itself, so the row only says what changed.
            row.AddChild(RemoveButton(() =>
            {
                _properties.RemoveAt(index);
                ApplyList(node);
            }));

            return row;
        }

        /// <summary>A rename that would empty a name or repeat one already on the node is put back rather
        /// than written: two rows under one name are one number, and which of the two survives is a coin
        /// toss the author never asked for.</summary>
        private void CommitFieldName(PassiveNode node, LineEdit field, int index, string text)
        {
            if (_canvas is null || index >= _properties.Count) return;

            string wanted = text.Trim();
            if (string.Equals(wanted, _properties[index].Key, StringComparison.Ordinal)) return;

            if (wanted.Length == 0 || Holds(wanted))
            {
                field.Text = _properties[index].Key;
                return;
            }

            // Named per row, like the value box beside it: renaming one field and then another is two
            // steps back rather than one.
            _properties[index] = new KeyValuePair<string, float>(wanted, _properties[index].Value);
            ApplyProperties(node, $"{EditFields.PropertyName} {index}");
            RebuildLater();
        }

        /// <summary>Hands the whole ordered list to the door that records it, and tells the totals.</summary>
        private void ApplyProperties(PassiveNode node, string field)
        {
            _editor.SetProperties(node, _properties, field);
            ChangedTotals();
        }

        /// <summary>A row appearing or disappearing is one gesture and one step, so the run is closed as
        /// part of it: removing a field and then adding one are two steps back rather than one.</summary>
        private void ApplyList(PassiveNode node)
        {
            _editor.SetProperties(node, _properties, EditFields.PropertyList);
            _editor.Seal();
        }

        private bool Holds(string name) => _properties.Exists(row => string.Equals(row.Key, name, StringComparison.Ordinal));

        /// <summary>A name no row is using yet, so a fresh row never collides with one already there.</summary>
        private string FreshFieldName()
        {
            if (!Holds(NewFieldName)) return NewFieldName;

            for (int suffix = 2; ; suffix++)
                if (!Holds($"{NewFieldName}{suffix}")) return $"{NewFieldName}{suffix}";
        }

        private static Label Warned(string text)
        {
            Label label = EditorControls.Wrapped(text);
            label.AddThemeColorOverride("font_color", CanvasStyle.Gold1);
            return label;
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

        /// <summary>Both channels share the node's line budget, so both buttons close at the same count —
        /// and both close outright on a node that grants a passive, which is the other half of the rule
        /// that shuts the passive field on a node carrying lines.</summary>
        private Button AddButton(string text, PassiveNode node, NodeKindRule rule, Action add)
        {
            var button = new Button
            {
                Text = text,
                Disabled = node.IsPassive || node.LineCount >= rule.MaxModifiers,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                TooltipText = node.IsPassive ? PassiveHoldsTheNode : string.Empty
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
