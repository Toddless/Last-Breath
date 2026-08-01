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
    /// book exactly while an <see cref="PassiveNodeKind.AbilityUnlock"/> node carrying it is taken.
    /// Taking the node learns it, giving the node back forgets it, and a character who has spent
    /// nothing knows nothing. Every pass brings the book all the way back to the allocation, so an
    /// ability put there by any other hand does not survive the next one.
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

        /// <summary>Node ability ids the catalog does not know, already reported. Reconcile runs on
        /// every allocation change and defensively from the read handlers, so a content gap has to be
        /// said once instead of once per pass.</summary>
        private readonly HashSet<string> _reportedGaps = new(StringComparer.Ordinal);

        /// <param name="passiveTree">Optional: the tree service belongs to the game project, and a
        /// project composed without it (the battle sandbox) resolves the default.</param>
        public AbilityUnlockService(
            IPlayerAccessor playerAccessor,
            IAbilityProvider abilityProvider,
            IPassiveTreeService? passiveTree = null)
        {
            _playerAccessor = playerAccessor;
            _abilityProvider = abilityProvider;
            _passiveTree = passiveTree;

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
        /// Brings the book to exactly what the taken nodes hand out: what no node backs any more is
        /// forgotten first, so the slots it held are free before the new abilities take theirs.
        /// Idempotent — a pass that finds the book already matching changes nothing, which is what
        /// lets the read handlers call it defensively.
        /// </summary>
        /// <param name="notify">Not read. An ability arrives because the player spent a point on the
        /// node carrying it, and the node lighting up says so already; there is nothing left for a
        /// toast to announce.</param>
        public void Reconcile(bool notify)
        {
            IAbilityBookComponent? book = _playerAccessor.Player?.AbilityBook;
            if (book == null) return;

            HashSet<string> granted = GrantedAbilityIds();
            ForgetRevoked(book, granted);
            LearnGranted(book, granted);
        }

        /// <summary>The ability ids the taken nodes hand out right now.</summary>
        private HashSet<string> GrantedAbilityIds()
        {
            HashSet<string> granted = new(StringComparer.Ordinal);
            if (_passiveTree == null) return granted;

            PassiveTreeDocument tree = _passiveTree.Tree;
            foreach (string nodeId in _passiveTree.TakenNodes)
            {
                PassiveNode? node = tree.Find(nodeId);
                if (node is not { Kind: PassiveNodeKind.AbilityUnlock }) continue;
                if (IsLearnable(node)) granted.Add(node.AbilityId);
            }

            return granted;
        }

        /// <summary>Whether the node's ability may enter a player's book. An id the catalog does not
        /// know is a content gap — the node gives nothing and says so once, rather than throwing on
        /// the first attempt to build the ability. A hidden ability is skipped without a word: boss
        /// reactions are cast internally, and the tree must not become a road into the book for one.</summary>
        private bool IsLearnable(PassiveNode node)
        {
            if (_abilityProvider.KnownAbilityIds.Contains(node.AbilityId))
                return !_abilityProvider.IsHidden(node.AbilityId);

            if (_reportedGaps.Add(node.AbilityId))
                Tracker.TrackNotFound($"Ability '{node.AbilityId}' unlocked by passive node '{node.Id}'");

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
    }
}
