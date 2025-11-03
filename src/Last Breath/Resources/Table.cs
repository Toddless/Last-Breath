namespace LastBreath.Resource
{
    using System.Collections.Generic;
    using Godot;

    public partial class Table<T> : Node
        where T : class
    {
        protected static Dictionary<string, T> Elements { get; } = [];

        public bool TryGetElement(string id, out T? element) => Elements.TryGetValue(id, out element);

        public virtual List<string> GetAllElements(string id) => [];

        public virtual void AddNewElement(T element) { }

        protected virtual void LoadData() { }
    }
}
