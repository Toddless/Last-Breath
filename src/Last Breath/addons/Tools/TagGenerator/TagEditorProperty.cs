#if TOOLS
namespace LastBreath.addons.Tools.TagGenerator
{
    using System.Collections.Generic;
    using Godot;
    using Godot.Collections;

    [Tool]
    public partial class TagEditorProperty : EditorProperty
    {
        private HBoxContainer _mainBox;
        private Label _label;
        private Button _editBtn;

        private PopupPanel _popup;
        private VBoxContainer _popupVBox;
        private Tree _tree;
        private HBoxContainer _buttonsBox;
        private Button _applyBtn;
        private Button _cancelBtn;

        private string _registryDir = string.Empty;

        public TagEditorProperty()
        {
            _mainBox = new HBoxContainer();
            _label = new Label() { Text = "Tags" };
            _editBtn = new Button() { Text = "Edit" };
            _editBtn.Pressed += OnEditPressed;
            _mainBox.AddChild(_label);
            _mainBox.AddChild(_editBtn);
            AddChild(_mainBox);

            _popup = new PopupPanel();

            _popupVBox = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            _tree = new Tree
            {
                HideRoot = true,
                Columns = 1,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _popupVBox.AddChild(_tree);

            _applyBtn = new Button() { Text = "Apply" };
            _cancelBtn = new Button() { Text = "Cancel" };
            _applyBtn.Pressed += OnApplyPressed;
            _cancelBtn.Pressed += () => _popup.Hide();

            _buttonsBox = new HBoxContainer();
            _buttonsBox.AddChild(_applyBtn);
            _buttonsBox.AddChild(_cancelBtn);

            var popupVBox = new VBoxContainer();
            popupVBox.AddChild(_popupVBox);
            popupVBox.AddChild(_buttonsBox);

            _popup.AddChild(popupVBox);
            AddChild(_popup);
        }

        public void SetRegistryDir(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _registryDir = path;
            }
        }

        public override void _UpdateProperty()
        {
            BuildTreeFromRegistry();
            SelectCurrentValues();
        }

        private void OnEditPressed()
        {
            BuildTreeFromRegistry();
            SelectCurrentValues();
            _popup.PopupCentered(new Vector2I(500, 420));
        }

        private void OnApplyPressed()
        {
            var selected = new Array<string>();

            var root = _tree.GetRoot();

            if (root != null)
            {
                var stack = new Stack<TreeItem>();
                var child = root.GetFirstChild();
                while (child != null)
                {
                    stack.Push(child);
                    child = child.GetNext();
                }

                while (stack.Count > 0)
                {
                    var treeItem = stack.Pop();

                    if (treeItem.IsChecked(0))
                    {
                        var meta = treeItem.GetMetadata(0);
                        if (meta.VariantType == Variant.Type.String)
                        {
                            var dataAsString = meta.AsString() ?? string.Empty;
                            selected.Add(dataAsString);
                        }
                    }

                    var treeChild = treeItem.GetFirstChild();
                    while (treeChild != null)
                    {
                        stack.Push(treeChild);
                        treeChild = treeChild.GetNext();
                    }
                }
            }

            var edited = GetEditedObject();
            var prop = GetEditedProperty();
            if (edited != null && prop != null)
            {
                edited.Set(prop, selected);
                EmitChanged(prop, selected);
                if (edited is Resource r) r.EmitChanged();
            }

            _popup.Hide();
        }

        private void BuildTreeFromRegistry()
        {
            _tree.Clear();

            var reg = ResourceLoader.Load<TagRegistry>(_registryDir);
            var rootItem = _tree.CreateItem();

            var created = new System.Collections.Generic.Dictionary<string, TreeItem>();

            TreeItem? CreateNode(string id, TreeItem parent)
            {
                if (string.IsNullOrEmpty(id)) return null;
                if (created.TryGetValue(id, out var exist)) return exist;

                var treeItem = _tree.CreateItem(parent);
                var def = reg.GetDefinition(id);
                treeItem.SetEditable(0, true);
                treeItem.SetCellMode(0, TreeItem.TreeCellMode.Check);
                treeItem.SetText(0, def?.DisplayName ?? id);
                treeItem.SetMetadata(0, id);

                created[id] = treeItem;

                foreach (var child in reg.GetChildren(id))
                    CreateNode(child, treeItem);

                return treeItem;
            }

            // Create roots
            foreach (var id in reg.GetAllIds())
            {
                if (string.IsNullOrWhiteSpace(reg.GetParent(id))) CreateNode(id, rootItem);
            }

            // no roots? Create flat list
            if (_tree.GetRoot().GetFirstChild() == null)
            {
                foreach (var id in reg.GetAllIds())
                    CreateNode(id, rootItem);
            }
        }

        private void SelectCurrentValues()
        {
            var edited = GetEditedObject();
            var prop = GetEditedProperty();
            if (edited == null || prop == null)
                return;

            var val = edited.Get(prop);
            var current = new HashSet<string>();

            try
            {

                if (val.VariantType == Variant.Type.Array)
                {
                    foreach (var v in val.AsGodotArray<string>())
                        if (!string.IsNullOrEmpty(v)) current.Add(v);
                }
                else
                {
                    foreach (var s in val.AsStringArray())
                        if (!string.IsNullOrEmpty(s)) current.Add(s);
                }
            }
            catch
            {
                foreach (var v in val.AsGodotArray<Variant>())
                {
                    if (v.VariantType == Variant.Type.Nil) continue;
                    var asString = v.AsString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(asString)) current.Add(asString);
                }
            }

            var root = _tree.GetRoot();
            if (root == null) return;

            var stack = new Stack<TreeItem>();
            var it = root.GetFirstChild();
            while (it != null)
            {
                stack.Push(it);
                it = it.GetNext();
            }

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                var meta = item.GetMetadata(0);
                var id = meta.VariantType != Variant.Type.Nil ? meta.ToString() : item.GetText(0);
                if (!string.IsNullOrEmpty(id) && current.Contains(id))
                    item.SetChecked(0, true);
                else
                    item.SetChecked(0, false);

                var child = item.GetFirstChild();
                while (child != null)
                {
                    stack.Push(child);
                    child = child.GetNext();
                }
            }
        }
    }
}
#endif
