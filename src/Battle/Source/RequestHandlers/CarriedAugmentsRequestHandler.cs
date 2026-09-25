namespace Battle.Source.RequestHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Abilities;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Views;
    using Godot;

    /// <summary>
    /// Reads the carried augments out of the bag — all of them for the tray, or the ones a named slot
    /// would take for the picker that slot opens. Filtered on the INSTANCES rather than on the tag
    /// index: an index answers with record ids, and what a drag carries — or a pick names — is the id
    /// of one copy.
    /// <para>
    /// The two answers differ only in WHICH copies, and that half of the second is not written here:
    /// the install gate is asked, so the list a slot offers is the list it would accept. One mapping
    /// serves both, so a tile in the tray and the same augment in a picker never disagree.
    /// </para>
    /// </summary>
    /// <param name="augments">What the record says — everything about the augment that is the same for
    /// every copy of it, of which the tray shows the tier.</param>
    /// <param name="inventory">Optional: a composition without a bag carries nothing.</param>
    /// <param name="gate">Optional, for the same reason: a composition with no slots to seat into
    /// offers no candidates for one.</param>
    /// <param name="art">Where a picture comes from. Injectable because loading one is an engine call,
    /// and a host without the engine must be able to ask for the tray without one.</param>
    public class CarriedAugmentsRequestHandler(
        IAbilityAugmentCatalog augments,
        IInventory? inventory = null,
        IAugmentInstallGate? gate = null,
        Func<string, Texture2D?>? art = null)
        : IRequestHandler<GetCarriedAugmentsRequest, IReadOnlyList<AugmentTrayTileView>>,
            IRequestHandler<GetAugmentCandidatesRequest, IReadOnlyList<AugmentTrayTileView>>
    {
        private readonly Func<string, Texture2D?> _art = art ?? AbilityArt.LoadIcon;

        public Task<IReadOnlyList<AugmentTrayTileView>> HandleRequest(GetCarriedAugmentsRequest request)
        {
            IReadOnlyList<AugmentTrayTileView> tiles =
            [
                .. (inventory?.GetContents() ?? [])
                    .Select(entry => entry.Item)
                    .OfType<IAugmentItem>()
                    .OrderBy(item => item.Id, StringComparer.Ordinal)
                    .ThenBy(item => item.InstanceId, StringComparer.Ordinal)
                    .Select(ToTile)
            ];

            return Task.FromResult(tiles);
        }

        /// <summary>The offer of one slot, in the gate's own order: nothing is sorted or filtered again
        /// on the way out, or the window would be showing a list of its own making.</summary>
        public Task<IReadOnlyList<AugmentTrayTileView>> HandleRequest(GetAugmentCandidatesRequest request)
        {
            IReadOnlyList<AugmentTrayTileView> tiles =
                [.. (gate?.Candidates(request.SocketAddress) ?? []).Select(ToTile)];

            return Task.FromResult(tiles);
        }

        /// <summary>One tile. Name, numbers and rarity belong to the COPY; tier, tags and binding are read
        /// off the record, because where an augment fits is the same for every copy of it — and a copy
        /// whose record the catalog no longer declares simply says none of it.</summary>
        private AugmentTrayTileView ToTile(IAugmentItem item)
        {
            AbilityAugmentData? record = augments.Find(item.Id);
            return new AugmentTrayTileView(
                item.InstanceId,
                item.Id,
                item.DisplayName,
                item.Description,
                _art(item.Id),
                item.Rarity,
                record?.Tier ?? 0,
                record?.Tags ?? [],
                record?.AbilityId ?? string.Empty,
                record?.FitsAnyAbility ?? false);
        }
    }
}
