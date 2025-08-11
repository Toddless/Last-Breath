#if TOOLS
namespace LastBreath.addons.Tools.TagGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;
    using Godot.Collections;

    [Tool]
    public partial class TagEditorProperty : EditorProperty
    {
        private HBoxContainer _mainBox;
        private Label _label;
        private Button _openBtn;

        private PopupPanel _popup;
        private VBoxContainer _popupVBox;
        private Tree _tree;
        private HBoxContainer _buttonsBox;
        private Button _applyBtn;
        private Button _cancelBtn;

        private string _registryDir = string.Empty;
        private RegistryData? _cachedRegistryData = null;
        private struct RegistryData
        {
            public System.Collections.Generic.Dictionary<string, string> IdToDisplay;
            public System.Collections.Generic.Dictionary<string, List<string>> ChildrenById;
            public List<string> Roots;
            public Func<string, string> GetParent;
        }

        public TagEditorProperty()
        {
            _mainBox = new HBoxContainer();
            _label = new Label() { Text = "Tags" };
            _openBtn = new Button() { Text = "Edit" };
            _openBtn.Pressed += OnOpenPressed;

            _mainBox.AddChild(_label);
            _mainBox.AddChild(_openBtn);
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

        private void OnOpenPressed()
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

            var data = _cachedRegistryData ?? LoadRegistryData(_registryDir);

            var idToDisplay = data.IdToDisplay;
            var childrenById = data.ChildrenById;
            var roots = data.Roots;

            var rootItem = _tree.CreateItem();

            var itemMap = new System.Collections.Generic.Dictionary<string, TreeItem>();
            var creating = new HashSet<string>();

            TreeItem? CreateOrGetItem(string id)
            {
                if (string.IsNullOrEmpty(id)) return null;
                if (itemMap.TryGetValue(id, out var existing)) return existing;
                if (creating.Contains(id)) return null;
                creating.Add(id);

                string parent = data.GetParent.Invoke(id);

                TreeItem parentItem = rootItem;
                if (!string.IsNullOrEmpty(parent))
                {
                    var pi = CreateOrGetItem(parent);
                    if (pi != null)
                        parentItem = pi;
                }

                var it = _tree.CreateItem(parentItem);

                var display = idToDisplay.TryGetValue(id, out string? value) ? value : id;
                it.SetEditable(0, true);
                it.SetCellMode(0, TreeItem.TreeCellMode.Check);
                it.SetText(0, display);
                it.SetMetadata(0, id);
                itemMap[id] = it;
                creating.Remove(id);
                return it;
            }

            foreach (var rootId in roots)
            {
                CreateOrGetItem(rootId);
                void EnsureChildren(string parentId)
                {
                    if (!childrenById.TryGetValue(parentId, out var list)) return;
                    foreach (var child in list)
                    {
                        CreateOrGetItem(child);
                        EnsureChildren(child);
                    }
                }
                EnsureChildren(rootId);
            }

            if (roots.Count == 0)
            {
                foreach (var id in idToDisplay.Keys)
                {
                    if (!itemMap.ContainsKey(id))
                        CreateOrGetItem(id);
                }
            }
        }


        private RegistryData LoadRegistryData(string registryDir)
        {
            var idToDisplay = new System.Collections.Generic.Dictionary<string, string>();
            var childrenById = new System.Collections.Generic.Dictionary<string, List<string>>();
            var parentById = new System.Collections.Generic.Dictionary<string, string?>(); 
            var roots = new List<string>();

            var dir = DirAccess.Open(registryDir);
            if (dir == null)
            {
                GD.PrintErr($"Directory not found: {registryDir}");
                return new RegistryData
                {
                    IdToDisplay = idToDisplay,
                    ChildrenById = childrenById,
                    Roots = roots,
                    GetParent = s => parentById.TryGetValue(s, out var p) ? p ?? string.Empty : string.Empty,
                };
            }

            dir.ListDirBegin();
            while (true)
            {
                var file = dir.GetNext();
                if (string.IsNullOrWhiteSpace(file)) break;
                if (file.StartsWith('.')) continue;
                if (!(file.EndsWith(".tres") || file.EndsWith(".res"))) continue;

                var full = $"{registryDir.TrimEnd('/')}/{file}";
                var res = ResourceLoader.Load(full);
                if (res == null)
                {
                    GD.PrintErr("Tag resource not loaded");
                    continue;
                }

                if (res is TagRegistry reg)
                {
                    // Tags
                    if (reg.AllTags.Length > 0)
                    {
                        foreach (var tag in reg.AllTags)
                        {
                            if (tag == null) continue;
                            var id = TagUtils.Normalize(tag.Id);
                            if (string.IsNullOrEmpty(id)) continue;
                            if (!idToDisplay.ContainsKey(id))
                                idToDisplay[id] = string.IsNullOrEmpty(tag.DisplayName) ? tag.Id : tag.DisplayName;
                        }
                    }

                    // Hierarchies
                    if (reg.Hierarchies.Length > 0)
                    {
                        foreach (var raw in reg.Hierarchies)
                        {
                            if (string.IsNullOrWhiteSpace(raw)) continue;
                            var parts = raw.Split(['/', '\\', '>', '.'], StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim())
                                           .Where(s => s.Length > 0)
                                           .ToArray();

                            if (parts.Length == 0) continue;

                            string? prevId = null;
                            for (int i = 0; i < parts.Length; i++)
                            {
                                var seg = parts[i];
                                var id = TagUtils.Normalize(seg);
                                if (!idToDisplay.ContainsKey(id))
                                    idToDisplay[id] = seg;

                                if (prevId == null)
                                {
                                    // root candidate
                                    if (!roots.Contains(id))
                                        roots.Add(id);
                                    // ensure maps exist
                                    if (!childrenById.ContainsKey(id))
                                        childrenById[id] = [];
                                    if (!parentById.ContainsKey(id))
                                        parentById[id] = null;
                                }
                                else
                                {
                                    if (!childrenById.ContainsKey(prevId))
                                        childrenById[prevId] = [];
                                    if (!childrenById[prevId].Contains(id))
                                        childrenById[prevId].Add(id);

                                    parentById[id] = prevId;

                                    if (!childrenById.ContainsKey(id))
                                        childrenById[id] = [];
                                }

                                prevId = id;
                            }
                        }
                    }

                }
            }
            dir.ListDirEnd();

            var realRoots = new List<string>();
            foreach (var r in roots)
            {
                if (!parentById.ContainsKey(r) || string.IsNullOrEmpty(parentById[r]))
                    realRoots.Add(r);
            }
            if (realRoots.Count == 0)
            {
                realRoots = [.. idToDisplay.Keys];
            }

            return new RegistryData
            {
                IdToDisplay = idToDisplay,
                ChildrenById = childrenById,
                Roots = realRoots,
                GetParent = s => parentById.TryGetValue(s, out var p) ? p ?? string.Empty : string.Empty,
            };
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
