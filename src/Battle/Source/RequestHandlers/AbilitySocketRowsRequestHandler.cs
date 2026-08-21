namespace Battle.Source.RequestHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Services;
    using Core.Views;
    using Godot;

    /// <summary>
    /// Builds the socket sheet: one row per ability with slots, cells straight off the board. No domain
    /// object leaves, and no number is worked out here — cost, cooldown and description are read off the
    /// live ability instance, which already wears what its sockets hold.
    /// <para>
    /// The reconcile pass the old stance-tree handler ran defensively is deliberately NOT run: this
    /// handler is what a panel calls when the board tells it something changed, and a reconcile inside
    /// it would sync the board again and close the loop.
    /// </para>
    /// </summary>
    /// <param name="minter">Turns a seated copy back into the thing it would be in the bag, which is
    /// where a cell's name, description and rarity come from. Asked rather than reproduced: the player
    /// must read the same augment in the slot as he read in his bag.</param>
    /// <param name="augments">Where the tier of a seated copy comes from — the record's, read out of the
    /// same catalog the tray reads it out of, because an augment's tier is one number however the player
    /// happens to be looking at it. Optional: a composition supplying no records prints no tier.</param>
    /// <param name="art">Where a picture comes from. Injectable because loading one is an engine call,
    /// and a host without the engine (a walk over the sheet's composition) must be able to ask for the
    /// rows without one.</param>
    public class AbilitySocketRowsRequestHandler(
        IAbilitySocketBoard sockets,
        IPlayerAccessor players,
        IAbilityProvider abilities,
        IAugmentItemMinter minter,
        IAbilityAugmentCatalog? augments = null,
        Func<string, Texture2D?>? art = null)
        : IRequestHandler<GetAbilitySocketRowsRequest, IReadOnlyList<AbilitySocketRowView>>
    {
        private readonly Func<string, Texture2D?> _art = art ?? AbilityArt.LoadIcon;

        public Task<IReadOnlyList<AbilitySocketRowView>> HandleRequest(GetAbilitySocketRowsRequest request)
        {
            Dictionary<string, IAbility> owned = OwnedAbilities();

            IReadOnlyList<AbilitySocketRowView> rows =
            [
                .. Subjects(owned.Keys)
                    .Where(abilityId => Matches(request, abilityId))
                    .OrderBy(abilityId => abilityId, StringComparer.Ordinal)
                    .Select(abilityId => ToRow(abilityId, owned.GetValueOrDefault(abilityId)))
            ];

            return Task.FromResult(rows);
        }

        private static string CostOf(IAbility ability) =>
            Localization.Render("UI_AbilityCost", new Dictionary<string, object?>
            {
                ["Value"] = ability.CostValue,
                ["Resource"] = Localization.Localize(ability.CostType.ToString()),
            });

        private static string CooldownOf(IAbility ability) =>
            Localization.Render("UI_AbilityCooldown", new Dictionary<string, object?>
            {
                ["Value"] = Mathf.RoundToInt(ability.Cooldown),
            });

        /// <summary>How much of the seated augment is running, asked of the live instance — the one place
        /// the winner of two augments reaching for one parameter is decided. A row without an instance
        /// prints no numbers at all, so it has no dormancy to report either.</summary>
        private static AugmentActivity ActivityOf(IAbility? owned, AbilitySocket socket) =>
            owned?.ActivityOf(socket.Address) ?? AugmentActivity.Working;

        /// <summary>
        /// Which abilities get a row: every one holding a slot of any kind — a live one or a closed one
        /// still holding the player's augment — plus every one the book holds. The first half is what
        /// keeps an ability whose node was refunded on screen while his property is still in it; the
        /// second is a guarantee that an ability reaching the book by some other road is not invisible.
        /// An id the catalog no longer declares is skipped: there is no stance to file it under.
        /// </summary>
        private IEnumerable<string> Subjects(IEnumerable<string> ownedIds) =>
            sockets.Sockets
                .Select(socket => socket.AbilityId)
                .Concat(ownedIds)
                .Where(abilities.KnownAbilityIds.Contains)
                .Distinct(StringComparer.Ordinal);

        private bool Matches(GetAbilitySocketRowsRequest request, string abilityId) =>
            (request.Stance == null || abilities.GetAbilityStance(abilityId) == request.Stance)
            && (request.AbilityId == null || string.Equals(request.AbilityId, abilityId, StringComparison.Ordinal));

        /// <summary>The learned abilities by id. An instance is what carries the LIVE numbers, so a row
        /// without one prints no numbers at all rather than the catalog's undecorated bases.</summary>
        private Dictionary<string, IAbility> OwnedAbilities()
        {
            Dictionary<string, IAbility> owned = new(StringComparer.Ordinal);
            foreach (IAbility ability in players.Player?.AbilityBook.AllAbilities ?? [])
                owned[ability.Id] = ability;

            return owned;
        }

        /// <summary>One row. Name, art and stance come from the catalog whether or not the ability is
        /// still owned — a row the player kept only because his augments are in it has no instance to
        /// read anything off.</summary>
        private AbilitySocketRowView ToRow(string abilityId, IAbility? owned) =>
            new(
                abilityId,
                owned?.DisplayName ?? Localization.Localize(abilityId),
                owned?.Description ?? string.Empty,
                owned == null ? string.Empty : CostOf(owned),
                owned == null ? string.Empty : CooldownOf(owned),
                _art(abilityId),
                abilities.GetAbilityStance(abilityId),
                owned != null,
                [.. sockets.SocketsOf(abilityId).Select(socket => ToCell(owned, socket))]);

        /// <summary>One cell. An augment whose record the catalog no longer declares cannot be turned
        /// back into a thing — the minter says so by handing back nothing — and the cell then shows the
        /// slot as filled with an unnamed occupant rather than as free, because free is exactly what it
        /// is not: something is in there, and the player can still take it out.</summary>
        private AugmentCellView ToCell(IAbility? owned, AbilitySocket socket)
        {
            if (socket.Augment is not { } augment)
                return new AugmentCellView(
                    socket.Address, AugmentCellKind.Empty, socket.Tier, 0,
                    string.Empty, string.Empty, string.Empty, null, Rarity.Common, AugmentActivity.Working);

            IAugmentItem? carried = minter.Restore(augment);
            return new AugmentCellView(
                socket.Address,
                socket.IsOpen ? AugmentCellKind.Filled : AugmentCellKind.Held,
                socket.Tier,
                augments?.Find(augment.AugmentId)?.Tier ?? 0,
                augment.AugmentId,
                carried?.DisplayName ?? Localization.Localize(augment.AugmentId),
                carried?.Description ?? string.Empty,
                _art(augment.AugmentId),
                carried?.Rarity ?? Rarity.Common,
                ActivityOf(owned, socket));
        }
    }
}
