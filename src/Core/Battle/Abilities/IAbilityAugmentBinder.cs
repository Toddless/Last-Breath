namespace Core.Battle.Abilities
{
    /// <summary>
    /// The one place where what stands in the sockets becomes what the abilities actually do. The board
    /// holds the arrangement and the ability holds the behaviour, and nothing else in the game is
    /// allowed to join the two: an ability is upgraded by exactly the augments in its slots, by exactly
    /// the numbers those copies rolled, and by nothing a window, a handler or a save file decided on
    /// its own.
    ///
    /// The board is changed down more roads than the player's own hands — an allocation that gave a
    /// node back drops the socket and its occupant with it, a load lays the saved arrangement out, a
    /// new playthrough empties the board — so every one of those roads ends here rather than each
    /// working out for itself which upgrade to take off which ability.
    /// </summary>
    public interface IAbilityAugmentBinder
    {
        /// <summary>
        /// Makes every ability in the player's book wear exactly the augments its sockets hold. Total
        /// rather than incremental: the caller says that the board has moved, not what moved in it, so
        /// an ability whose slot was emptied by any road loses the upgrade in the same pass that gives
        /// another one its new augment. Idempotent — a pass over an unchanged board leaves the same
        /// arrangement standing.
        /// </summary>
        void Bind();
    }
}
