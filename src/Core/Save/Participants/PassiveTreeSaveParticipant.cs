namespace Core.Save.Participants
{
    using System.Collections.Generic;
    using Data.SaveData;
    using Newtonsoft.Json.Linq;
    using PassiveTree.Allocation;

    /// <summary>
    /// Persists the tree allocation as bare ids. Nodes the catalog no longer knows are reported and
    /// dropped instead of failing the load: the tree is content, and content is rewritten between
    /// builds while saves outlive it. The service re-checks whatever survives that filter, so a set
    /// that lost its route to a seed arrives trimmed rather than as an allocation the rules forbid.
    /// </summary>
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

        /// <summary>Keeps the ids the tree catalog still contains and reports the rest. The spend is
        /// recounted from the set that survives, so the point a dropped node cost comes back to the
        /// remainder — the player is left holding budget for a node that no longer exists, not a
        /// charge for one.</summary>
        private List<string> KnownNodes(IReadOnlyCollection<string> saved)
        {
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
