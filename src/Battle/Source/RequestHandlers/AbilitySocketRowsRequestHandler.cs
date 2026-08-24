namespace Battle.Source.RequestHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Services;
    using Core.Views;
    using Core.Views.UI;
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
    /// <param name="augments">Where the tier, the tags and the binding of a seated copy come from — the
    /// record's, read out of the same catalog the tray reads them out of, because what an augment is and
    /// where it fits are the same however the player happens to be looking at it. Optional: a composition
    /// supplying no records prints neither tier nor fitting.</param>
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
                .. Subjects(owned.Keys, request.IncludeUnowned)
                    .Where(abilityId => Matches(request, abilityId))
                    .OrderBy(abilityId => abilityId, StringComparer.Ordinal)
                    .Select(abilityId => ToRow(abilityId, owned.GetValueOrDefault(abilityId), request.IncludeUnowned))
            ];

            return Task.FromResult(rows);
        }

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
        /// <para>Asked for the unowned too, it is simply the whole catalog — every id above is already in
        /// it — minus the abilities no player ever casts. A hidden one is a boss reaction: it has no node,
        /// so a card of it would be a page about something unreachable.</para>
        /// </summary>
        private IEnumerable<string> Subjects(IEnumerable<string> ownedIds, bool includeUnowned) =>
            (includeUnowned
                ? abilities.KnownAbilityIds.Where(abilityId => !abilities.IsHidden(abilityId))
                : sockets.Sockets
                    .Select(socket => socket.AbilityId)
                    .Concat(ownedIds)
                    .Where(abilities.KnownAbilityIds.Contains))
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

        /// <summary>
        /// One row. Everything the player reads — name, price, wait, tags, description — is read off ONE
        /// instance, so a card is assembled the same way whoever is looking at it. Which instance is the
        /// only question: the book's where the player owns the ability, because that one wears his
        /// augments, and otherwise the one built for the reading.
        /// <para>Art, stance and ownership stay outside that: they are the catalog's answer and the book's,
        /// and neither of them is a number an instance decorates.</para>
        /// </summary>
        private AbilitySocketRowView ToRow(string abilityId, IAbility? owned, bool readUnowned)
        {
            IAbility? reading = owned ?? (readUnowned ? Preview(abilityId) : null);
            return new AbilitySocketRowView(
                abilityId,
                reading?.DisplayName ?? Localization.Localize(abilityId),
                reading?.Description ?? string.Empty,
                reading == null ? string.Empty : AbilityText.CostLine(reading.CostValue, reading.CostType),
                reading == null ? string.Empty : AbilityText.CooldownLine(reading.Cooldown),
                reading?.Tags ?? [],
                _art(abilityId),
                abilities.GetAbilityStance(abilityId),
                owned != null,
                [.. sockets.SocketsOf(abilityId).Select(socket => ToCell(owned, socket))]);
        }

        /// <summary>
        /// The ability as the catalog alone declares it, built to be read and thrown away. Nothing is
        /// learned and nothing is bound: a bare instance already words its own description off its own
        /// numbers, which is exactly the undecorated card of an ability the player has not bought yet.
        /// <para>Null and reported where the registry cannot build one — a record with no factory behind
        /// it parses and is listed, and the wheel walks every id there is.</para>
        /// </summary>
        private IAbility? Preview(string abilityId)
        {
            try
            {
                return abilities.CreateAbility(abilityId);
            }
            catch (Exception exception)
            {
                Tracker.TrackException($"Cannot read the card of ability '{abilityId}'", exception, this);
                return null;
            }
        }

        /// <summary>One cell. An augment whose record the catalog no longer declares cannot be turned
        /// back into a thing — the minter says so by handing back nothing — and the cell then shows the
        /// slot as filled with an unnamed occupant rather than as free, because free is exactly what it
        /// is not: something is in there, and the player can still take it out.</summary>
        private AugmentCellView ToCell(IAbility? owned, AbilitySocket socket)
        {
            if (socket.Augment is not { } augment)
                return new AugmentCellView(
                    socket.Address, AugmentCellKind.Empty, socket.Tier, 0,
                    string.Empty, string.Empty, string.Empty, null, Rarity.Common, AugmentActivity.Working,
                    [], string.Empty, false);

            IAugmentItem? carried = minter.Restore(augment);
            AbilityAugmentData? record = augments?.Find(augment.AugmentId);
            return new AugmentCellView(
                socket.Address,
                socket.IsOpen ? AugmentCellKind.Filled : AugmentCellKind.Held,
                socket.Tier,
                record?.Tier ?? 0,
                augment.AugmentId,
                carried?.DisplayName ?? Localization.Localize(augment.AugmentId),
                carried?.Description ?? string.Empty,
                _art(augment.AugmentId),
                carried?.Rarity ?? Rarity.Common,
                ActivityOf(owned, socket),
                record?.Tags ?? [],
                record?.AbilityId ?? string.Empty,
                record?.FitsAnyAbility ?? false);
        }
    }
}
