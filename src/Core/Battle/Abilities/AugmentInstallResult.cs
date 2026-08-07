namespace Core.Battle.Abilities
{
    /// <summary>
    /// Which gate an install stopped at. The fitting rule is only one of them — the bag may hold no
    /// such augment and the slot may not exist — so the reasons the rule owns are NOT repeated here:
    /// they travel beside this as <see cref="AugmentFitResult"/>, and a second list of them would be
    /// a second copy of the rule's vocabulary, free to fall behind the rule itself.
    /// </summary>
    public enum AugmentInstallOutcome
    {
        /// <summary>The augment left the bag and sits in the slot.</summary>
        Installed,

        /// <summary>The bag holds no augment under that instance id. Nothing moved.</summary>
        AugmentNotHeld,

        /// <summary>No slot of that id is open.</summary>
        NoSuchSocket,

        /// <summary>An augment already stands in the slot. Extraction is the only way out of one, so
        /// an install never puts the previous occupant out of the game.</summary>
        SocketOccupied,

        /// <summary>The slot does not take that augment. <see cref="AugmentInstallResult.Fit"/> says
        /// why, in the rule's own words.</summary>
        DoesNotFit
    }

    /// <summary>
    /// What came of moving an augment out of the bag and into a slot. A refusal changes nothing at
    /// all: the augment stays in the bag, and the caller is told which gate answered and — where the
    /// fitting rule is the one that did — the verdict it gave, so a window can name the reason to the
    /// player instead of working the rule out a second time.
    /// </summary>
    /// <param name="Outcome">Which gate answered.</param>
    /// <param name="Fit">The fitting rule's verdict, carried whole from the one place the rule lives.
    /// Null wherever the install never reached the rule — including a slot open for an augment no
    /// record declares, which the board refuses without asking a rule that would have nothing to
    /// measure.</param>
    public readonly record struct AugmentInstallResult(AugmentInstallOutcome Outcome, AugmentFitResult? Fit = null)
    {
        public bool Installed => Outcome == AugmentInstallOutcome.Installed;
    }
}
