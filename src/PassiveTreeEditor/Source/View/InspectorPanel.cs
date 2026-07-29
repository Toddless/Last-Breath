namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using Core.Enums;
    using Godot;
    using Io;
    using Model;

    /// <summary>
    /// Properties of the selected node. Rebuilt wholesale on every selection change — a node has a
    /// handful of controls, and rebuilding removes a whole class of stale-binding bugs that partial
    /// refreshes invite.
    /// </summary>
    public partial class InspectorPanel : VBoxContainer
    {
        private AbilityCatalog? _abilities;
        private TreeCanvas? _canvas;

        /// <summary>A cosmetic edit: the canvas needs a redraw, the totals do not.</summary>
        public event Action? NodeEdited;

        /// <summary>An edit that changes what the allocation is worth, or which nodes exist under
        /// which id. Kept separate so that typing a title does not rebuild the totals per keystroke.</summary>
        public event Action? TotalsChanged;

        public void Initialize(TreeCanvas canvas, AbilityCatalog abilities)
        {
            _canvas = canvas;
            _abilities = abilities;
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

        private OptionButton AbilityPicker(PassiveNode node)
        {
            var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            List<string> ids = ["", .. AbilityIds()];

            // A hand-written id that is not in the catalog stays selectable instead of being silently
            // reset — the tool never edits data it does not understand.
            if (!string.IsNullOrEmpty(node.AbilityId) && !ids.Contains(node.AbilityId)) ids.Add(node.AbilityId);

            int selected = 0;
            for (int index = 0; index < ids.Count; index++)
            {
                picker.AddItem(ids[index].Length == 0 ? "—" : ids[index], index);
                if (ids[index] == node.AbilityId) selected = index;
            }

            picker.Selected = selected;
            picker.ItemSelected += index =>
            {
                node.AbilityId = ids[picker.GetItemId((int)index)];
                ChangedTotals();
            };

            return picker;
        }

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
            AddChild(EditorControls.Caption($"MODIFIERS  {node.Modifiers.Count}/{rule.MaxModifiers}"
                                           + (node.IsHybrid ? "   hybrid" : string.Empty)));

            if (rule.MaxModifiers == 0 && node.Modifiers.Count == 0)
            {
                AddChild(EditorControls.Wrapped("This node class carries no modifier lines."));
                return;
            }

            // Lines left over from a kind change are still shown so they can be removed by hand. The
            // tool never silently drops authored content — Check reports the excess instead.
            for (int index = 0; index < node.Modifiers.Count; index++) AddChild(ModifierRow(node, index));

            var add = new Button { Text = "+ line", Disabled = node.Modifiers.Count >= rule.MaxModifiers };
            add.Pressed += () =>
            {
                node.Modifiers.Add(new ModifierLine());
                ChangedTotals();
                Rebuild();
            };
            AddChild(add);
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

            var remove = new Button { Text = "×" };
            remove.Pressed += () =>
            {
                node.Modifiers.RemoveAt(index);
                ChangedTotals();
                Rebuild();
            };
            top.AddChild(remove);

            var bottom = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            box.AddChild(bottom);

            // The hint label comes first: the value-type callback writes into it, so it has to exist
            // before the picker that captures it is built.
            var hint = new Label { CustomMinimumSize = new Vector2(64, 0) };

            OptionButton typePicker = EditorControls.Picker(ValueTypes(), line.ValueType, type =>
            {
                line.ValueType = type;
                hint.Text = ValueHint(line);
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
                hint.Text = ValueHint(line);
                ChangedTotals();
            };

            hint.Text = ValueHint(line);

            var condition = new LineEdit
            {
                Text = line.Condition,
                PlaceholderText = "condition (optional)",
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            condition.TextChanged += text =>
            {
                bool wasConditional = line.IsConditional;
                line.Condition = text;

                // Only the presence of a condition reaches the totals; its wording does not, so typing
                // inside an existing condition costs a redraw instead of a resummation.
                if (line.IsConditional == wasConditional) Changed();
                else ChangedTotals();
            };
            box.AddChild(condition);

            return box;
        }

        /// <summary>Flag is deliberately absent: it is legal on context parameters only, and a tree
        /// line always targets an <see cref="EntityParameter"/>.</summary>
        private static IEnumerable<ModifierValueType> ValueTypes() =>
        [
            ModifierValueType.Flat,
            ModifierValueType.Increase,
            ModifierValueType.Multiplicative
        ];

        private static string ValueHint(ModifierLine line) =>
            line.ValueType == ModifierValueType.Flat
                ? string.Empty
                : $"= {EditorControls.Percent(line.Value)}";
    }
}
