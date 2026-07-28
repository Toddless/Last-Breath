namespace PassiveTreeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
        /// <summary>Item id of the "no stance" entry — outside the enum so it can never be mistaken
        /// for a stance value.</summary>
        private const int NoStanceId = 100;

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
            foreach (Node child in GetChildren())
            {
                RemoveChild(child);
                child.QueueFree();
            }

            if (_canvas is null) return;

            if (_canvas.Selection.Count == 0)
            {
                AddChild(Hint("Nothing selected.\n\nSelect mode: click a node, drag to move, box-drag to\nmulti-select, Delete removes. Middle or right mouse\ndrag pans, wheel zooms, F frames the tree."));
                return;
            }

            if (_canvas.Selection.Count > 1)
            {
                AddChild(Hint($"{_canvas.Selection.Count} nodes selected.\nDrag moves them together; Delete removes them."));
                return;
            }

            PassiveNode? node = _canvas.SingleSelection;
            if (node is null) return;

            BuildIdentity(node);
            BuildText(node);
            BuildModifiers(node);
        }

        private static Label Hint(string text) => new()
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        private static Label Caption(string text)
        {
            var label = new Label { Text = text };
            label.AddThemeFontSizeOverride("font_size", 13);
            return label;
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
            AddChild(Caption("NODE"));

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
            var kindPicker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            int kindSelected = 0;
            int kindIndex = 0;
            foreach (PassiveNodeKind kind in Enum.GetValues<PassiveNodeKind>())
            {
                kindPicker.AddItem(kind.ToString(), (int)kind);
                if (kind == node.Kind) kindSelected = kindIndex;
                kindIndex++;
            }

            kindPicker.Selected = kindSelected;
            kindPicker.ItemSelected += index =>
            {
                node.Kind = (PassiveNodeKind)kindPicker.GetItemId((int)index);
                ChangedTotals();
                Rebuild();
            };
            grid.AddChild(kindPicker);

            grid.AddChild(new Label { Text = "Stance" });
            grid.AddChild(StancePicker(node.Stance, value =>
            {
                node.Stance = value;
                Changed();
                Rebuild();
            }));

            // The second ray only means something once there is a first one to bridge from.
            if (node.Stance is not null)
            {
                grid.AddChild(new Label { Text = "Hybrid" });
                grid.AddChild(StancePicker(node.HybridStance, value =>
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

        /// <summary>
        /// A stance dropdown with an explicit "no stance" entry. The entry carries a deliberately
        /// out-of-range id: Godot substitutes the item index for a negative id, and index 0 collides
        /// with <see cref="Stance.Dexterity"/> — which silently turned "—" into Dexterity.
        /// </summary>
        private static OptionButton StancePicker(Stance? current, Action<Stance?> apply)
        {
            var picker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            picker.AddItem("—", NoStanceId);

            int selected = 0;
            int index = 1;

            foreach (Stance stance in Enum.GetValues<Stance>())
            {
                picker.AddItem(stance.ToString(), (int)stance);
                if (current == stance) selected = index;
                index++;
            }

            picker.Selected = selected;
            picker.ItemSelected += choice =>
            {
                int id = picker.GetItemId((int)choice);
                apply(id == NoStanceId ? null : (Stance)id);
            };

            return picker;
        }

        private SpinBox PositionBox(PassiveNode node, bool horizontal)
        {
            var box = new SpinBox
            {
                MinValue = -100000,
                MaxValue = 100000,
                Step = 1,
                Value = horizontal ? node.X : node.Y,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

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
            AddChild(Caption(isRule ? "RULE TEXT" : "NOTES"));

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
            AddChild(Caption($"MODIFIERS  {node.Modifiers.Count}/{rule.MaxModifiers}"
                             + (node.IsHybrid ? "   hybrid" : string.Empty)));

            if (rule.MaxModifiers == 0 && node.Modifiers.Count == 0)
            {
                AddChild(Hint("This node class carries no modifier lines."));
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

            var parameterPicker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            int parameterSelected = 0;
            int parameterIndex = 0;
            foreach (EntityParameter parameter in Enum.GetValues<EntityParameter>())
            {
                parameterPicker.AddItem(parameter.ToString(), (int)parameter);
                if (parameter == line.Parameter) parameterSelected = parameterIndex;
                parameterIndex++;
            }

            parameterPicker.Selected = parameterSelected;
            parameterPicker.ItemSelected += pick =>
            {
                line.Parameter = (EntityParameter)parameterPicker.GetItemId((int)pick);
                ChangedTotals();
            };
            top.AddChild(parameterPicker);

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

            var hint = new Label { CustomMinimumSize = new Vector2(64, 0) };

            var typePicker = new OptionButton();
            int typeSelected = 0;
            int typeIndex = 0;
            foreach (ModifierValueType type in ValueTypes())
            {
                typePicker.AddItem(type.ToString(), (int)type);
                if (type == line.ValueType) typeSelected = typeIndex;
                typeIndex++;
            }

            typePicker.Selected = typeSelected;
            bottom.AddChild(typePicker);

            var value = new SpinBox
            {
                MinValue = -100000,
                MaxValue = 100000,
                Step = 0.001,
                Value = line.Value,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            bottom.AddChild(value);
            bottom.AddChild(hint);

            typePicker.ItemSelected += pick =>
            {
                line.ValueType = (ModifierValueType)typePicker.GetItemId((int)pick);
                hint.Text = ValueHint(line);
                ChangedTotals();
            };

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
                line.Condition = text;
                ChangedTotals();
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
                : $"= {(line.Value * 100f).ToString("0.##", CultureInfo.InvariantCulture)}%";
    }
}
