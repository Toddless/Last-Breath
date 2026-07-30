namespace Core.Save.Participants
{
    using System.Collections.Generic;
    using Data.SaveData;
    using Newtonsoft.Json.Linq;
    using PassiveTree;

    /// <summary>
    /// The slice of the passive-tree service the save file needs: which nodes are taken and how many
    /// points are left. Kept narrow on purpose — the participant has no business reaching into
    /// allocation rules, and a project that only stores the tree can satisfy this without them.
    /// </summary>
    public interface IPassiveTreeSaveState
    {
        /// <summary>Ids of every taken node, granted seeds included.</summary>
        IReadOnlyCollection<string> AllocatedNodes { get; }

        /// <summary>Points granted but not yet spent.</summary>
        int AvailablePoints { get; }

        /// <summary>Replaces the whole allocation. Wholesale rather than node-by-node: a partially
        /// applied save would leave the allocation in a shape the take/refund rules never produce.</summary>
        void RestoreState(IReadOnlyCollection<string> allocatedNodes, int availablePoints);
    }

    /// <summary>
    /// Persists the tree allocation as bare ids. Nodes the catalog no longer knows are reported and
    /// dropped instead of failing the load: the tree is content, and content is rewritten between
    /// builds while saves outlive it.
    /// </summary>
    public class PassiveTreeSaveParticipant(IPassiveTreeSaveState tree, IPassiveTreeProvider treeData) : ISaveParticipant
    {
        public string SectionId => "passiveTree";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.PassiveTree;

        public JToken Capture() => JToken.FromObject(new PassiveTreeSaveData
        {
            Allocated = [.. tree.AllocatedNodes],
            AvailablePoints = tree.AvailablePoints
        });

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<PassiveTreeSaveData>();
            if (saved == null) return;

            tree.RestoreState(KnownNodes(saved.Allocated), saved.AvailablePoints);
        }

        /// <summary>Keeps the ids the tree catalog still contains and reports the rest. The points a
        /// dropped node cost stay spent — refunding them here would hand out budget the player never
        /// earned, and a respec gives them back through the normal path.</summary>
        private List<string> KnownNodes(IReadOnlyCollection<string> saved)
        {
            List<string> known = new(saved.Count);

            foreach (string id in saved)
            {
                if (treeData.Tree.Contains(id))
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
