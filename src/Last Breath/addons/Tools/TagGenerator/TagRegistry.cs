namespace LastBreath.Addons.Tools.TagGenerator
{
    using System.Linq;
    using Godot;
    using Godot.Collections;

    [Tool]
    [GlobalClass]
    public partial class TagRegistry : Resource
    {
        private Dictionary<string, TagDefinition> _defById = [];
        private Dictionary<string, Array<string>> _childById = [];
        private Dictionary<string, string?> _parentById = [];
        private bool _built = false;

        [Export] public TagDefinition[] AllTags { get; set; } = [];
        [Export] public string[] Hierarchies { get; set; } = [];

        public TagDefinition? GetDefinition(string id)
        {
            EnsureBuilt();
            if (string.IsNullOrEmpty(id)) return null;
            _defById.TryGetValue(TagUtils.Normalize(id), out var td);
            return td;
        }

        public string[] GetAllIds()
        {
            EnsureBuilt();
            return [.. _defById.Keys];
        }

        public string[] GetChildren(string id)
        {
            EnsureBuilt();
            if (string.IsNullOrEmpty(id)) return [];
            var key = TagUtils.Normalize(id);
            if (_childById.TryGetValue(key, out var list))
                return [.. list];

            return [];
        }

        public string? GetParent(string id)
        {
            EnsureBuilt();
            if (string.IsNullOrEmpty(id)) return null;
            _parentById.TryGetValue(TagUtils.Normalize(id), out var parent);
            return parent;
        }

        private void EnsureBuilt()
        {
            if (_built ) return;

            foreach (var tag in AllTags)
            {
                // not sure about it
                if (tag == null) continue;
                var key = TagUtils.Normalize(tag.Id);
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!_defById.ContainsKey(key))
                    _defById[key] = tag;
                if (!_childById.ContainsKey(key))
                    _childById[key] = [];
            }

            foreach (var raw in Hierarchies)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var parts = raw.Split(['/', '\\', '>', '.'], System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToArray();

                if (parts.Length == 0) continue;

                string? prev = null;

                foreach (var part in parts)
                {
                    var id = TagUtils.Normalize(part);
                    if (string.IsNullOrEmpty(id)) continue;

                    if (!_childById.ContainsKey(id))
                        _childById[id] = [];
                    if (!_defById.ContainsKey(id))
                    {
                        var td = new TagDefinition { Id = id, DisplayName = part };
                        _defById[id] = td;
                    }

                    if (prev != null)
                    {
                        if (!_childById[prev].Contains(id))
                            _childById[prev].Add(id);
                        _parentById[id] = prev;
                    }
                    else
                    {
                        if (!_parentById.ContainsKey(id))
                            _parentById[id] = null;
                    }

                    prev = id;
                }
            }

            var keys = _defById.Keys.ToArray();

            foreach (var key in keys)
                if (!_childById.ContainsKey(key))
                    _childById[key] = [];

            _built = true;
        }
    }
}
