namespace Playground.Resource
{
    using Godot;
    using System.Collections.Generic;

    public partial class Table<T> : Node
        where T : class
    {
        protected static Dictionary<string, T> Elements { get; } = [];

        public bool TryGetElement(string id, out T? element) => Elements.TryGetValue(id, out element);

        public virtual T? GetValue(string id) => TryGetElement(id, out T? element) ? element : null;

        public virtual List<string> GetAllElements(string id) => [];

        public virtual void AddNewElement(T element) { }

        protected virtual void LoadData() { }
    }
}
