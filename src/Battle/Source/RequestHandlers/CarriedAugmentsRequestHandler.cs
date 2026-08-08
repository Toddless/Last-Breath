namespace Battle.Source.RequestHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Views;
    using Godot;

    /// <summary>
    /// Reads the carried augments out of the bag. Filtered on the INSTANCES rather than on the tag
    /// index: an index answers with record ids, and what a drag carries is the id of one copy.
    /// </summary>
    /// <param name="augments">What the record says — everything about the augment that is the same for
    /// every copy of it, of which the tray shows the tier.</param>
    /// <param name="inventory">Optional: a composition without a bag carries nothing.</param>
    /// <param name="art">Where a picture comes from. Injectable because loading one is an engine call,
    /// and a host without the engine must be able to ask for the tray without one.</param>
    public class CarriedAugmentsRequestHandler(
        IAbilityAugmentCatalog augments,
        IInventory? inventory = null,
        Func<string, Texture2D?>? art = null)
        : IRequestHandler<GetCarriedAugmentsRequest, IReadOnlyList<AugmentTrayTileView>>
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

        private AugmentTrayTileView ToTile(IAugmentItem item) =>
            new(
                item.InstanceId,
                item.Id,
                item.DisplayName,
                item.Description,
                _art(item.Id),
                item.Rarity,
                augments.Find(item.Id)?.Tier ?? 0);
    }
}
