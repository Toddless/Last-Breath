namespace Core.Battle.Abilities
{
    /// <summary>
    /// One augment slot of one ability. Identity is <see cref="SocketId"/> — the passive-tree node
    /// that opened it — and not the tier, so two nodes of the same tier pointing at the same ability
    /// are two sockets rather than one that overwrites itself.
    ///
    /// The socket owns the fact that something is in it, not the rules the occupant was made under:
    /// it holds the copy that went in, numbers and all. Which copy that is matters — two copies of one
    /// record are worth different amounts — so the slot keeps the one it was given rather than an id
    /// standing for any of them. What an augment is made of belongs to whoever mints augments
    /// (<see cref="AugmentMinter"/>).
    /// </summary>
    /// <param name="socketId">Identity, unique across the tree (the node's own id).</param>
    /// <param name="abilityId">The ability the slot belongs to.</param>
    /// <param name="tier">Which tier of augment the slot accepts. A plain number: nothing indexes
    /// by it, so a tier the game has not used yet costs no code.</param>
    public sealed class AbilitySocket(string socketId, string abilityId, int tier)
    {
        public string SocketId { get; } = socketId;

        public string AbilityId { get; } = abilityId;

        public int Tier { get; } = tier;

        /// <summary>The slot said the way a placement says it. Two slots are the same slot only when
        /// all three parts agree: an id whose ability or tier has moved names a different slot, and
        /// what was in the old one was not chosen for this one.</summary>
        public AbilitySocketPlacement Placement => new(SocketId, AbilityId, Tier);

        /// <summary>The augment copy occupying the socket; null while it is free.</summary>
        public AugmentInstance? Augment { get; private set; }

        public bool IsEmpty => Augment is null;

        /// <summary>Fills a free socket. An occupied one refuses rather than swapping: extraction is
        /// the only way out, so an install can never make the previous occupant disappear.
        /// The slot holds, it does not judge: whether the augment belongs here is decided once, by
        /// <see cref="AugmentFit"/>, and the board is what puts the question — everything the rule
        /// needs (the augment's record, the ability's tags, what its other slots wear) lives above a
        /// single socket.</summary>
        public bool Install(AugmentInstance augment)
        {
            if (!IsEmpty || string.IsNullOrWhiteSpace(augment?.AugmentId)) return false;

            Augment = augment;
            return true;
        }

        /// <summary>Empties the socket and hands back what came out — null when it was already free.
        /// The mirror of <see cref="Install"/>: the copy that went in is the copy that comes out, with
        /// the numbers it was minted with.</summary>
        public AugmentInstance? Extract()
        {
            AugmentInstance? augment = Augment;
            Augment = null;
            return augment;
        }
    }
}
