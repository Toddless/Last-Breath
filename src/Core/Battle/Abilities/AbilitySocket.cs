namespace Core.Battle.Abilities
{
    /// <summary>
    /// One augment slot of one ability. Identity is the whole placement — the node that opened it, the
    /// ability it points at and the tier it takes (<see cref="Placement"/>, addressed as
    /// <see cref="Address"/>) — and not the tier alone, so two nodes of the same tier pointing at the
    /// same ability are two sockets rather than one that overwrites itself.
    ///
    /// The socket owns the fact that something is in it, not the rules the occupant was made under:
    /// it holds the copy that went in, numbers and all. Which copy that is matters — two copies of one
    /// record are worth different amounts — so the slot keeps the one it was given rather than an id
    /// standing for any of them. What an augment is made of belongs to whoever mints augments
    /// (<see cref="AugmentMinter"/>).
    ///
    /// A slot whose node was given back is CLOSED rather than destroyed while something is in it
    /// (<see cref="IsOpen"/>). The ability stops wearing that augment at once, but the augment itself is
    /// the player's property and the only way out of a slot is to take it out — a refund that swallowed
    /// what the player put in would be a loss he cannot undo by re-buying the node.
    /// </summary>
    /// <param name="socketId">The node that opened the slot. Part of the identity, not all of it.</param>
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

        /// <summary>How the slot is named outside the board — in a request, a view or a console line.</summary>
        public string Address => Placement.Address;

        /// <summary>Whether the allocation still backs the slot. A closed one is remove-only: what is
        /// in it can be taken out and nothing can be put in, and it disappears the moment it is
        /// empty.</summary>
        public bool IsOpen { get; private set; } = true;

        /// <summary>The augment copy physically occupying the socket, whether or not the slot is still
        /// backed by a node; null while it is free. This is what the save file writes, what a window
        /// draws and what an extraction hands back.</summary>
        public AugmentInstance? Augment { get; private set; }

        /// <summary>The augment the ABILITY wears — the occupant of an open slot, and nothing at all
        /// from a closed one. Split from <see cref="Augment"/> on purpose: the two questions have
        /// different readers, and a single property would leave every one of them free to forget the
        /// filter and hand the player a refunded node's upgrade for free.</summary>
        public AugmentInstance? WorkingAugment => IsOpen ? Augment : null;

        public bool IsEmpty => Augment is null;

        /// <summary>Fills a free socket. A closed one refuses whatever is offered — the node behind it
        /// is gone, so it is a place an augment is taken OUT of and never put into, and the refusal
        /// stands here rather than in the gates above so that every road obeys it, a load included.
        /// An occupied one refuses rather than swapping: extraction is the only way out, so an install
        /// can never make the previous occupant disappear.
        /// The slot holds, it does not judge: whether the augment belongs here is decided once, by
        /// <see cref="AugmentFit"/>, and the board is what puts the question — everything the rule
        /// needs (the augment's record, the ability's tags, what its other slots wear) lives above a
        /// single socket.</summary>
        public bool Install(AugmentInstance augment)
        {
            if (!IsOpen || !IsEmpty || string.IsNullOrWhiteSpace(augment?.AugmentId)) return false;

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

        /// <summary>The node behind the slot is gone. What is in it stays where it is and stops
        /// working; an empty slot is not closed but dropped, so a closed socket always has something
        /// to give back.</summary>
        public void Close() => IsOpen = false;

        /// <summary>The node came back under the same signature. The slot works again, with whatever
        /// was left in it — the player bought back exactly the place his augment was sitting in.</summary>
        public void Reopen() => IsOpen = true;
    }
}
