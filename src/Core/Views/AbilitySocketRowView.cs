namespace Core.Views
{
    using System.Collections.Generic;
    using Enums;
    using Godot;

    /// <summary>
    /// One ability of the socket sheet: the ability on the left, its slots in a row beside it. Pure
    /// display data — no domain object leaves.
    /// </summary>
    /// <param name="AbilityId">Which ability the row is about.</param>
    /// <param name="DisplayName">Its name.</param>
    /// <param name="Description">Its LIVE description, with the augments in its slots already counted
    /// into every number. Empty for a row the player no longer owns the ability of: there is no
    /// instance to read numbers off, only the catalog's name and art.</param>
    /// <param name="Cost">The live cast price, rendered. Empty on an unowned row.</param>
    /// <param name="Cooldown">The live cooldown, rendered. Empty on an unowned row and on a cast that
    /// makes its caster wait for nothing.</param>
    /// <param name="Tags">What the cast counts as — the compatibility axis the augments fit along. Read
    /// off the live instance where there is one, so a tag an augment GRANTS is named; off the catalog's
    /// record otherwise, because an unowned ability still counts as what it is.</param>
    /// <param name="Icon">The ability's art; null when it has none yet.</param>
    /// <param name="Stance">Which stance section the row belongs to.</param>
    /// <param name="IsOwned">Whether the player still holds the ability. False rows exist because their
    /// slots still hold his augments — the row stays until he has taken the last one out, dimmed and
    /// remove-only.</param>
    /// <param name="Cells">The slots, in the order the board gives them: ascending tier, repeats legal,
    /// length whatever the allocation opened. Nothing anywhere assumes three of them.</param>
    /// <param name="CostValue">The live cast price as a bare number — the structured half of
    /// <paramref name="Cost"/>, for the card that captions and tints the value itself. Zero on a row
    /// with no instance behind it; <paramref name="CostPool"/> is what says whether zero is a price.</param>
    /// <param name="CostPool">What the price is paid in; null on a row with no instance behind it,
    /// which is how a reader tells "costs nothing" from "knows no cost".</param>
    /// <param name="CooldownTurns">The live cooldown in whole turns — the structured half of
    /// <paramref name="Cooldown"/>. Zero both for a cast without one and for a row with no instance:
    /// either way there is no wait to print.</param>
    /// <param name="Target">Whom the cast lands on, already worded — the wording knows the concrete
    /// targeting strategies, which live in the battle module beside the handler that fills this in.
    /// Empty on a row with no instance behind it.</param>
    public record AbilitySocketRowView(
        string AbilityId,
        string DisplayName,
        string Description,
        string Cost,
        string Cooldown,
        IReadOnlyList<string> Tags,
        Texture2D? Icon,
        Stance Stance,
        bool IsOwned,
        IReadOnlyList<AugmentCellView> Cells,
        int CostValue = 0,
        Costs? CostPool = null,
        float CooldownTurns = 0f,
        string Target = "");
}
