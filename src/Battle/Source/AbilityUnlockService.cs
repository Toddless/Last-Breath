namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.PassiveTree;
    using Core.PassiveTree.Allocation;
    using Core.Services;

    /// <summary>
    /// The player's ability book is the mirror of his passive-tree allocation: an ability is in the
    /// book exactly while a node that unlocks it (<see cref="NodeKindRules.UnlocksAbility"/>) is taken
    /// — an unlock node bought with a point, or the stance's seed, which is granted with the character
    /// and is why a fresh fighter starts with one ability per stance. Taking the node learns it,
    /// giving the node back forgets it, and a character who has taken nothing knows nothing. Every
    /// pass brings the book all the way back to the allocation, so an ability put there by any other
    /// hand does not survive the next one.
    ///
    /// The augment sockets travel the same road and in the same pass: a node whose class opens a
    /// slot (<see cref="NodeKindRules.SocketTier"/>) puts that slot on the board while it is taken,
    /// and the slot goes when the node does. One reading of the taken set answers for both, so the
    /// book and the board can never be looking at different allocations.
    ///
    /// Two things move under the book, so both are listened to: the allocation (a purchase, a refund,
    /// a respec, a session reset, a restored save) and the player himself — a new game or a scene
    /// change builds a second player with an empty book in the same process. The constructor runs the
    /// same pass, because the player is a scene node two levels under the one whose _Ready resolves
    /// this service, and Godot readies children first: that first announcement is always missed.
    ///
    /// A project that registers no tree service grants nothing at all.
    /// </summary>
    public class AbilityUnlockService : IDisposable, IAbilityUnlockService
    {
        private readonly IPlayerAccessor _playerAccessor;
        private readonly IAbilityProvider _abilityProvider;
        private readonly IPassiveTreeService? _passiveTree;
        private readonly IAbilitySocketBoard? _sockets;

        /// <summary>Ability ids named by a node and missing from the catalog, already reported.
        /// Reconcile runs on every allocation change and defensively from the read handlers, so a
        /// content gap has to be said once instead of once per pass.</summary>
        private readonly HashSet<string> _reportedGaps = new(StringComparer.Ordinal);

        /// <param name="passiveTree">Optional: the tree service belongs to the game project, and a
        /// project composed without it (the battle sandbox) resolves the default.</param>
        /// <param name="sockets">Optional: a project without a socket board simply has no augment
        /// slots, the same way one without a tree has no abilities.</param>
        public AbilityUnlockService(
            IPlayerAccessor playerAccessor,
            IAbilityProvider abilityProvider,
            IPassiveTreeService? passiveTree = null,
            IAbilitySocketBoard? sockets = null)
        {
            _playerAccessor = playerAccessor;
            _abilityProvider = abilityProvider;
            _passiveTree = passiveTree;
            _sockets = sockets;

            _playerAccessor.PlayerChanged += OnPlayerChanged;
            if (_passiveTree != null) _passiveTree.AllocationChanged += OnAllocationChanged;

            Reconcile(notify: false);
        }

        public void Dispose()
        {
            _playerAccessor.PlayerChanged -= OnPlayerChanged;
            if (_passiveTree != null) _passiveTree.AllocationChanged -= OnAllocationChanged;
        }

        /// <summary>
        /// Brings the book and the socket board to exactly what the taken nodes hand out: what no
        /// node backs any more is forgotten first, so the slots it held are free before the new
        /// abilities take theirs. Idempotent — a pass that finds both already matching changes
        /// nothing, which is what lets the read handlers call it defensively.
        /// The sockets are synced whether or not there is a player: they hang off the allocation, and
        /// a scene without a fighter in it yet must not leave the board holding an older one.
        /// A pass that could not read an allocation at all changes nothing, in either channel: an
        /// unread tree is not an empty one, and treating it as empty would let a launch whose document
        /// failed to parse take the board's contents down with it.
        /// </summary>
        /// <param name="notify">Not read. An ability arrives because the player spent a point on the
        /// node carrying it, and the node lighting up says so already; there is nothing left for a
        /// toast to announce.</param>
        public void Reconcile(bool notify)
        {
            TreeGrants granted = ReadGrants();
            if (!granted.AllocationRead) return;

            _sockets?.Sync(granted.Sockets);

            IAbilityBookComponent? book = _playerAccessor.Player?.AbilityBook;
            if (book == null) return;

            ForgetRevoked(book, granted.Abilities);
            LearnGranted(book, granted.Abilities);
        }

        /// <summary>Everything the taken nodes hand out right now, read in one pass so the abilities
        /// and the slots can never come from two different readings of the allocation. A project
        /// composed without a tree reads an allocation that is empty by construction; a tree whose
        /// document has no content reads none at all.</summary>
        private TreeGrants ReadGrants()
        {
            var granted = new TreeGrants(new HashSet<string>(StringComparer.Ordinal), [], AllocationRead: true);
            if (_passiveTree == null) return granted;

            PassiveTreeDocument tree = _passiveTree.Tree;
            if (tree.IsEmpty) return granted with { AllocationRead = false };

            foreach (string nodeId in _passiveTree.TakenNodes)
            {
                PassiveNode? node = tree.Find(nodeId);
                if (node != null && PointsAtAnAbility(node) && ReferencesAPlayerAbility(node)) Collect(node, granted);
            }

            return granted;
        }

        /// <summary>
        /// Whether the node says anything about an ability at all: its class opens something on one
        /// (<see cref="NodeKindRules.SocketTier"/>) and the node names which. Content nodes carry lines
        /// and an empty ability field, and putting that field to the catalog would turn every one of
        /// them into a reported gap. The neutral seed at the core of the wheel leaves the field empty
        /// for the same reason it belongs to no stance — it opens nothing — so it hands out neither an
        /// ability nor a slot, and says nothing about it: whether a node of an ability-bearing class
        /// had to name one depends on where the node sits, which is the authoring tool's question.
        /// </summary>
        private static bool PointsAtAnAbility(PassiveNode node) =>
            NodeKindRules.SocketTier(node.Kind) != NodeKindRules.NoSocket && !string.IsNullOrWhiteSpace(node.AbilityId);

        /// <summary>A node whose class unlocks hands the ability over; any node with a tier opens that
        /// slot on it. The unlocking classes do both — the tier-1 slot comes with the ability rather
        /// than with a node of its own — and which classes those are is the per-class table's answer,
        /// the same one the authoring tool checks the tree against.</summary>
        private static void Collect(PassiveNode node, TreeGrants granted)
        {
            if (NodeKindRules.UnlocksAbility(node.Kind)) granted.Abilities.Add(node.AbilityId);

            granted.Sockets.Add(new AbilitySocketPlacement(node.Id, node.AbilityId, NodeKindRules.SocketTier(node.Kind)));
        }

        /// <summary>Whether the ability the node names is one a player may hold — the same question for
        /// an unlock and for a socket, since a slot on an ability nobody can own is worth as little as
        /// the ability. An id the catalog does not know is a content gap: the node gives nothing and
        /// says so once, rather than throwing on the first attempt to build the ability. A hidden
        /// ability is skipped without a word — boss reactions are cast internally, and the tree must
        /// not become a road into the book for one.</summary>
        private bool ReferencesAPlayerAbility(PassiveNode node)
        {
            if (_abilityProvider.KnownAbilityIds.Contains(node.AbilityId))
                return !_abilityProvider.IsHidden(node.AbilityId);

            if (_reportedGaps.Add(node.AbilityId))
                Tracker.TrackNotFound($"Ability '{node.AbilityId}' referenced by passive node '{node.Id}'");

            return false;
        }

        private static void ForgetRevoked(IAbilityBookComponent book, IReadOnlySet<string> granted)
        {
            List<IAbility> revoked = [.. book.AllAbilities.Where(ability => !granted.Contains(ability.Id))];
            foreach (IAbility ability in revoked) book.Forget(ability.InstanceId);
        }

        /// <summary>Learns in id order so the slots a batch of abilities lands in are the same on every
        /// run — a restored allocation arrives as a set, and a set has no order of its own.</summary>
        private void LearnGranted(IAbilityBookComponent book, IReadOnlySet<string> granted)
        {
            HashSet<string> learned = [.. book.AllAbilities.Select(ability => ability.Id)];

            foreach (string abilityId in granted.Where(id => !learned.Contains(id)).OrderBy(id => id, StringComparer.Ordinal))
                book.Learn(_abilityProvider.GetAbilityStance(abilityId), _abilityProvider.CreateAbility(abilityId));
        }

        private void OnAllocationChanged() => Reconcile(notify: false);

        private void OnPlayerChanged(IPlayer _) => Reconcile(notify: false);

        /// <summary>What one reading of the taken set produced.</summary>
        /// <param name="AllocationRead">Whether there was an allocation to read. False only for a tree
        /// service holding a document with no content: the taken ids have nothing to be measured
        /// against, so the pass reports no grants and no revocations either — the difference between
        /// "this character has nothing" and "nobody could be asked".</param>
        private readonly record struct TreeGrants(
            HashSet<string> Abilities,
            List<AbilitySocketPlacement> Sockets,
            bool AllocationRead);
    }
}
