namespace Battle.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Constants;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Services;
    using Core.Views;
    using Godot;

    /// <summary>
    /// Fills the mastery tree: every ability of the stance (from the provider, not just learned ones),
    /// ordered by unlock threshold, with state derived from the current mastery level. Reconcile runs
    /// first so late-loaded ability data / a freshly set player are reflected.
    /// </summary>
    public class GetStanceAbilityRequestHandler(
        IAbilityProvider abilityProvider,
        IMartialArtMastery mastery,
        IAbilityUnlockService unlockService)
        : IRequestHandler<GetStanceAbilityRequest, IReadOnlyList<AbilitySlotView>>
    {
        public Task<IReadOnlyList<AbilitySlotView>> HandleRequest(GetStanceAbilityRequest request)
        {
            unlockService.Reconcile(notify: false);

            var views = abilityProvider.KnownAbilityIds
                .Where(id => abilityProvider.GetAbilityStance(id) == request.Stance)
                .OrderBy(abilityProvider.GetMasteryLevel)
                .Select(ToView)
                .ToList();
            return Task.FromResult<IReadOnlyList<AbilitySlotView>>(views);
        }

        private AbilitySlotView ToView(string abilityId)
        {
            int unlockLevel = abilityProvider.GetMasteryLevel(abilityId);
            var state = mastery.CurrentLevel >= unlockLevel ? AbilityState.Available : AbilityState.Locked;
            return new AbilitySlotView(abilityId, LoadIcon(abilityId), state, unlockLevel);
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
