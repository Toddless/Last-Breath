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

        /// <summary>A file with no section of ours describes a character with no allocation. The
        /// service is a singleton that outlives the scene, so without this the nodes of the file
        /// loaded before it stay on a character who never bought them — and the next save writes
        /// them into a file that never had them.</summary>
        public void RestoreMissingSection() => tree.RestoreState([]);

        /// <summary>Keeps the ids the tree catalog still contains and reports the rest. The spend is
        /// recounted from the set that survives, so the point a dropped node cost comes back to the
        /// remainder — the player is left holding budget for a node that no longer exists, not a
        /// charge for one.
        /// While the catalog holds no tree at all there is nothing to check ids against: the set
        /// passes through untouched instead of being reported gone wholesale, and the service holds
        /// it until a document arrives to check it.</summary>
        private List<string> KnownNodes(IReadOnlyCollection<string> saved)
        {
            if (tree.Tree.Nodes.Count == 0) return [.. saved];

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
