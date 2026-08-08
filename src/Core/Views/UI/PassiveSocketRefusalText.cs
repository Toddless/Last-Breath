namespace Core.Views.UI
{
    /// <summary>
    /// Why an augment cannot be dropped onto a slot drawn on the passive wheel, in one localisation key.
    ///
    /// <para>These two refusals belong to the WHEEL and not to the install gate, and they have to be
    /// answered before it. A slot the allocation has not opened does not exist on the board at all, so
    /// the gate would honestly answer "there is no such socket" — and send the player looking for a slot
    /// when what he is missing is a point. A slot whose node is marked to go back does exist and would
    /// take the augment, and then close around it the moment the plan is applied.</para>
    ///
    /// <para>A table of the interface's own, the way <see cref="AugmentRefusalText"/> and
    /// <see cref="PassiveTreeRefusalText"/> are: what must not exist is a second reading of the RULE, and
    /// there is none — the two facts are read off the allocation and the plan.</para>
    /// </summary>
    public static class PassiveSocketRefusalText
    {
        public const string NotOpened = "UI_PassiveTree_Socket_NotOpened";

        public const string PlannedRefund = "UI_PassiveTree_Socket_PlannedRefund";

        /// <param name="openerTaken">The node that opens the slot is in the character's allocation.</param>
        /// <param name="plannedReturn">The plan means to give that node back.</param>
        /// <returns>The key naming the refusal, or null when the wheel has nothing of its own to say and
        /// the install gate answers.</returns>
        public static string? KeyFor(bool openerTaken, bool plannedReturn)
        {
            if (!openerTaken) return NotOpened;

            return plannedReturn ? PlannedRefund : null;
        }
    }
}
