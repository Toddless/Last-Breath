namespace Core.Save.Participants
{
    using System.Collections.Generic;
    using Data.SaveData;
    using Newtonsoft.Json.Linq;
    using PassiveTree.Allocation;

    /// <summary>Persists the tree allocation as bare ids. Unknown nodes (content changed between builds)
    /// are reported and dropped rather than failing the load; the service then re-validates the survivors
    /// so a set that lost its route to a seed arrives trimmed, not as a forbidden allocation.</summary>
    public class PassiveTreeSaveParticipant(IPassiveTreeService tree) : ISaveParticipant
    {
        public string SectionId => "passiveTree";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.PassiveTree;

        public JToken Capture() => JToken.FromObject(new PassiveTreeSaveData { Allocated = [.. tree.TakenNodes] });

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<PassiveTreeSaveData>();
            if (saved == null) return;

            tree.RestoreState(KnownNodes(saved.Allocated));
        }

        /// <summary>No section means no allocation. Needed because the service is a singleton that
        /// outlives the scene and would otherwise keep a prior file's nodes on a character who never
        /// bought them.</summary>
        public void RestoreWithoutSection() => tree.RestoreState([]);

        /// <summary>Keeps ids the catalog still contains, reports the rest, and lets spend re-count from
        /// the survivors so a dropped node's point returns to the budget instead of staying spent. If the
        /// catalog holds no tree yet, the set passes through unchecked rather than being reported gone
        /// wholesale.</summary>
        private List<string> KnownNodes(IReadOnlyCollection<string> saved)
        {
            if (tree.Tree.IsEmpty) return [.. saved];

            List<string> known = new(saved.Count);

            foreach (string id in saved)
            {
                if (tree.Tree.Contains(id))
                {
                    known.Add(id);
                    continue;
                }

                Tracker.TrackNotFound($"Passive tree node '{id}' from the save");
            }

            return known;
        }
    }
}
