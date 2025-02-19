using System.Collections.Generic;
using Godot;

namespace Playground.Script.Helpers
{
    public static class NodeFinder
    {
        private static readonly Dictionary<string, Node> s_cache = [];

        public static Node? FindBFSCached(Node root, string target)
        {
            if (s_cache.TryGetValue(target, out Node? cachedNode))
                return cachedNode;

            var result = FindNodeBFS(root, target);
            if (result != null)
            {
                s_cache[target] = result;
            }
            return result;
        }

        public static void ClearCache() => s_cache.Clear();

        private static Node? FindNodeBFS(Node root, string target)
        {
            if (root == null) return null;
            Queue<Node> queue = new();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                Node current = queue.Dequeue();
                if (current.Name == target) return current;

                foreach (var child in current.GetChildren())
                {
                    queue.Enqueue(child);
                }
            }
            return null;
        }
    }
}
