namespace Core.Battle.Abilities
{
    /// <summary>
    /// What came of putting an ornament on an ability. Every answer but <see cref="Attached"/> moves
    /// nothing: the ornament stays in the bag and the ability wears what it wore.
    /// </summary>
    public enum OrnamentAttachResult
    {
        /// <summary>The ornament left the bag and the ability has its extra socket.</summary>
        Attached,

        /// <summary>The ornament is already on an ability — this one or another. Answered before the bag
        /// is asked, because "you are not carrying it" is true and useless: what the player needs to hear
        /// is which ability to take it off.</summary>
        AlreadyAttached,

        /// <summary>The bag holds no ornament under that instance id.</summary>
        OrnamentNotHeld,

        /// <summary>No such ability, or one the player has not unlocked. The single gate the design puts
        /// on an ornament: an open socket of the same tier on the tree is expressly NOT required, so a
        /// tier-3 ornament may go onto an ability the build never invested in.</summary>
        AbilityNotUnlocked,

        /// <summary>The ability already wears an ornament. The whole rule in one word: at most one to an
        /// ability, or the fourth socket becomes a sixth and one button does everything.</summary>
        AbilityAlreadyOrnamented,

        /// <summary>The board refused for a reason the gate's own order does not account for. Unreachable
        /// through the checks above and reported as itself rather than borrowed from one of them: a wrong
        /// label sends whoever reads it looking for the wrong thing, and this answer at least says the
        /// refusal came from below.</summary>
        BoardRefused
    }

    /// <summary>
    /// What came of taking an ornament off an ability. Every answer but <see cref="Detached"/> leaves it
    /// where it was.
    /// </summary>
    public enum OrnamentDetachResult
    {
        /// <summary>The ornament is off the ability and back in the bag; the socket it granted is gone
        /// with it.</summary>
        Detached,

        /// <summary>No ability wears that ornament.</summary>
        NotAttached,

        /// <summary>Its socket still holds an augment. Remove-only, like a slot whose node was given
        /// back: the augment comes out on its own road, so that the fitting rule judges it again against
        /// whatever ability it lands on next.</summary>
        SocketNotEmpty,

        /// <summary>The bag has no room for it. The ornament stays on the ability rather than being
        /// handed to a bag that would drop it.</summary>
        NoBagRoom,

        /// <summary>The ornament cannot be turned back into a thing to carry: no record declares it any
        /// more. It stays where it is, still granting the socket it granted.</summary>
        CannotBeHeld
    }
}
