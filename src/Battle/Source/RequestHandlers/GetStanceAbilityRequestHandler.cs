namespace Battle.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
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
            return new AbilitySlotView(abilityId,  ResourceLoader.Load<Texture2D>($"res://Internal/_Placeholders/Icons/{abilityId}.png"), state, unlockLevel);
        }
    }
}
