namespace Playground.Resource
{
    using System.Collections.Generic;
    using Godot;

    public partial class Table<T> : Node
        where T : class
    {
        private static readonly Dictionary<string, T> s_elements = [];

        protected static Dictionary<string, T> Elements => s_elements;

        public static bool TryGetElement(string id, out T? element) => s_elements.TryGetValue(id, out element);

        protected virtual void LoadData()
        {

        }
    }
}
