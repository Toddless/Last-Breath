namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Entity;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Services;

    /// <summary>
    /// The twin of <see cref="AbilityUnlockService"/> on the other channel. A taken node naming a passive
    /// (<see cref="PassiveNode.PassiveId"/>) puts that passive on the player, built on the spot from the
    /// node's own numbers; giving the node back takes it off. Every pass brings the player's roster back to
    /// what the allocation hands out, so a passive this service did not grant is never touched and one it
    /// did grant never outlives its node.
    ///
    /// <para>What was handed out is written down node by node, and the ledger holds the very instance the
    /// roster received — the same bookkeeping unequipping an item does with its grant. One passive id may
    /// arrive from several sources at once (an item, an NPC kit, another node), and only an instance-exact
    /// revocation can take back this one without taking theirs.</para>
    ///
    /// <para>Nothing of this is saved: the grants ARE the allocation, and the allocation is what the file
    /// carries. A restored allocation announces itself through
    /// <see cref="IPassiveTreeService.AllocationChanged"/> — the same signal a purchase, a refund, a
    /// respec, a session reset and a reloaded document raise — so a load reconciles by the same road as a
    /// click. The player is listened to for the same reason the book's service listens to him: a new game
    /// or a scene change builds a second player with an empty roster in the same process.</para>
    ///
    /// <para>A project that registers no tree service grants nothing at all.</para>
    /// </summary>
    public sealed class PassiveGrantService : IDisposable
    {
        private readonly IPlayerAccessor _playerAccessor;
        private readonly ISkillProvider _skills;
        private readonly IPassiveTreeService? _passiveTree;

        /// <summary>What each taken node was charged with handing over, keyed by node id — a node is the
        /// unit of the grant, not the passive id: two nodes may name one passive with different numbers.</summary>
        private readonly Dictionary<string, GrantedPassive> _granted = new(StringComparer.Ordinal);

        private IFightable? _owner;

        /// <param name="passiveTree">Optional: the tree service belongs to the game project, and a project
        /// composed without it (the battle sandbox) resolves the default.</param>
        public PassiveGrantService(
            IPlayerAccessor playerAccessor,
            ISkillProvider skills,
            IPassiveTreeService? passiveTree = null)
        {
            _playerAccessor = playerAccessor;
            _skills = skills;
            _passiveTree = passiveTree;

            _playerAccessor.PlayerChanged += OnPlayerChanged;
            if (_passiveTree != null) _passiveTree.AllocationChanged += OnAllocationChanged;

            Reconcile();
        }

        public void Dispose()
        {
            _playerAccessor.PlayerChanged -= OnPlayerChanged;
            if (_passiveTree != null) _passiveTree.AllocationChanged -= OnAllocationChanged;
        }

        /// <summary>
        /// Brings the player's passives to exactly what the taken nodes hand out. What no node backs any
        /// more comes off first, so a node re-tuned under the ledger is off before its replacement goes on.
        /// Idempotent — a pass that finds the roster already matching changes nothing, which is what makes
        /// a second reconcile cost one comparison instead of a second copy of every passive.
        /// A pass that could not read an allocation at all changes nothing: an unread tree is not an empty
        /// one, and treating it as empty would strip a character whose document failed to parse.
        /// </summary>
        public void Reconcile()
        {
            if (!TryReadTaken(out Dictionary<string, PassiveNode> taken)) return;

            FollowPlayer();
            if (_owner is not { } owner) return;

            Revoke(owner, taken);
            Grant(owner, taken);
        }

        /// <summary>The passive-bearing nodes the character holds right now, by node id. False only for a
        /// tree service holding a document with no content — the difference between "this character has
        /// nothing" and "nobody could be asked".</summary>
        private bool TryReadTaken(out Dictionary<string, PassiveNode> taken)
        {
            taken = new Dictionary<string, PassiveNode>(StringComparer.Ordinal);
            if (_passiveTree == null) return true;

            PassiveTreeDocument tree = _passiveTree.Tree;
            if (tree.IsEmpty) return false;

            foreach (string nodeId in _passiveTree.TakenNodes)
                if (tree.Find(nodeId) is { IsPassive: true } node)
                    taken[nodeId] = node;

            return true;
        }

        /// <summary>Moves the ledger onto the fighter it now describes. What the previous player wore is
        /// taken off him while he is still there to take it off, and the ledger is emptied — the new
        /// fighter's roster is his own, and a grant recorded against another character's would never be
        /// handed to him.</summary>
        private void FollowPlayer()
        {
            IPlayer? player = _playerAccessor.Player;
            if (ReferenceEquals(_owner, player)) return;

            if (_owner is { } previous) RemoveAll(previous);

            _granted.Clear();
            _owner = player;
        }

        private void RemoveAll(IFightable owner)
        {
            foreach (GrantedPassive granted in _granted.Values)
                if (granted.Skill is { } skill)
                    owner.PassiveSkills.RemoveSkill(skill);
        }

        /// <summary>Takes back exactly what was handed out and nothing else — by instance, so a passive
        /// another source also grants keeps its own registration. A node the character gave back and a node
        /// whose numbers changed under the ledger are both revoked here; the second is granted again below,
        /// with the numbers the document now carries.</summary>
        private void Revoke(IFightable owner, IReadOnlyDictionary<string, PassiveNode> taken)
        {
            string[] stale =
            [
                .. _granted
                    .Where(entry => !taken.TryGetValue(entry.Key, out PassiveNode? node) || !entry.Value.Matches(node))
                    .Select(entry => entry.Key)
            ];

            foreach (string nodeId in stale)
            {
                if (_granted[nodeId].Skill is { } skill) owner.PassiveSkills.RemoveSkill(skill);
                _granted.Remove(nodeId);
            }
        }

        /// <summary>Hands over what the ledger does not hold yet, in node-id order so a restored allocation
        /// — which arrives as a set, and a set has no order of its own — grants the same way on every run.
        /// A node the registry cannot build is written down as handing over nothing: the provider has
        /// already said why, and a content gap must be reported once rather than once per pass.</summary>
        private void Grant(IFightable owner, Dictionary<string, PassiveNode> taken)
        {
            IEnumerable<PassiveNode> fresh = taken
                .Where(entry => !_granted.ContainsKey(entry.Key))
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => entry.Value);

            foreach (PassiveNode node in fresh)
            {
                string passiveId = node.PassiveId!;
                ISkill? skill = _skills.CreateSkill(passiveId, new RecordProperties(passiveId, node.Properties));

                _granted[node.Id] = new GrantedPassive(passiveId, new Dictionary<string, float>(node.Properties), skill);
                if (skill != null) owner.PassiveSkills.AddSkill(skill);
            }
        }

        private void OnAllocationChanged() => Reconcile();

        private void OnPlayerChanged(IPlayer _) => Reconcile();

        /// <summary>One line of the ledger: what a node was read as, and the instance the roster received.</summary>
        /// <param name="Skill">Null when the registry could not build the passive — the node was read and
        /// handed over nothing, which is a fact worth remembering so the gap is not reported again. This
        /// holds only while the registry is a static table: once the passives it can build come from data,
        /// a reload can fill a gap this ledger has already written off, and a null needs retrying.</param>
        private sealed record GrantedPassive(
            string PassiveId,
            IReadOnlyDictionary<string, float> Properties,
            ISkill? Skill)
        {
            /// <summary>Whether the node still asks for exactly what was handed out. The numbers are part
            /// of the question: a document reloaded with a different value under a node the character still
            /// holds has to re-tune the passive, and a passive is tuned at birth.</summary>
            public bool Matches(PassiveNode node) =>
                string.Equals(PassiveId, node.PassiveId, StringComparison.Ordinal)
                && Properties.Count == node.Properties.Count
                && Properties.All(pair =>
                    node.Properties.TryGetValue(pair.Key, out float value) && value.Equals(pair.Value));
        }
    }
}
