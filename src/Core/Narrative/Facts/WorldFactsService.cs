namespace Core.Narrative.Facts
{
    using System;
    using System.Collections.Generic;

    public class WorldFactsService : IWorldFactsService, Session.ISessionResettable
    {
        private readonly Dictionary<string, int> _facts = [];

        public event Action<string, int>? FactChanged;

        public IReadOnlyDictionary<string, int> Snapshot => _facts;

        public bool IsSet(string key) => GetCount(key) > 0;

        public int GetCount(string key) => _facts.GetValueOrDefault(key);

        public void SetFact(string key)
        {
            if (IsSet(key)) return;
            Write(key, 1);
        }

        public void Add(string key, int amount = 1)
        {
            if (amount <= 0) return;
            Write(key, GetCount(key) + amount);
        }

        public void SetCount(string key, int value)
        {
            if (GetCount(key) == value) return;
            Write(key, value);
        }

        /// <summary>Silent wipe — no per-fact FactChanged storm on a session reset.</summary>
        public void ResetSession() => _facts.Clear();

        public void RestoreState(IReadOnlyDictionary<string, int> facts)
        {
            _facts.Clear();
            foreach ((string key, int count) in facts)
                _facts[key] = count;
        }

        private void Write(string key, int value)
        {
            _facts[key] = value;
            FactChanged?.Invoke(key, value);
        }
    }
}
