namespace Battle.Source.RequestHandlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Battle.Abilities;
    using Core.Constants;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Services;
    using Core.Views;
    using Godot;

    /// <summary>
    /// Fills the stance tree: every ability of the stance (from the provider, not just learned ones)
    /// in a stable ordinal order by id, available when the player's book already holds it. Reconcile
    /// runs first so late-loaded ability data / a freshly set player are reflected.
    /// </summary>
    public class GetStanceAbilityRequestHandler(
        IAbilityProvider abilityProvider,
        IPlayerAccessor playerAccessor,
        IAbilityUnlockService unlockService)
        : IRequestHandler<GetStanceAbilityRequest, IReadOnlyList<AbilitySlotView>>
    {
        public Task<IReadOnlyList<AbilitySlotView>> HandleRequest(GetStanceAbilityRequest request)
        {
            unlockService.Reconcile(notify: false);

            var learnedIds = LearnedIds();
            var views = abilityProvider.KnownAbilityIds
                .Where(id => !abilityProvider.IsHidden(id)) // boss reactions never surface in the tree
                .Where(id => abilityProvider.GetAbilityStance(id) == request.Stance)
                .OrderBy(id => id, StringComparer.Ordinal) // catalog enumeration order is not a contract
                .Select(id => ToView(id, learnedIds))
                .ToList();
            return Task.FromResult<IReadOnlyList<AbilitySlotView>>(views);
        }

        /// <summary>Ids the player already owns: the tree shows what the book holds, whatever granted it.</summary>
        private HashSet<string> LearnedIds() =>
            playerAccessor.Player?.AbilityBook.AllAbilities.Select(ability => ability.Id).ToHashSet() ?? [];

        private AbilitySlotView ToView(string abilityId, IReadOnlySet<string> learnedIds)
        {
            var state = learnedIds.Contains(abilityId) ? AbilityState.Available : AbilityState.Locked;
            return new AbilitySlotView(abilityId, LoadIcon(abilityId), state);
        }

        /// <summary>A missing icon is a report and an empty slot image, not an engine error.</summary>
        private static Texture2D? LoadIcon(string abilityId)
        {
            string path = AssetPaths.AbilityIcon(abilityId);
            if (ResourceLoader.Exists(path)) return ResourceLoader.Load<Texture2D>(path);

            Tracker.TrackNotFound($"Ability icon not found: {path}");
            GD.Print($"Ability icon not found: {path}");
            return null;
        }
    }
}
