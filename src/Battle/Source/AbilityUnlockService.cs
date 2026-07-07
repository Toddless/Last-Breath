namespace Battle.Source
{
    using System;
    using System.Linq;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Events;
    using Core.MessageBus;
    using Core.Save;
    using Core.Services;

    /// <summary>
    /// Owns the "mastery threshold -> ability becomes available" rule (variant A: reaching the
    /// threshold auto-learns the ability into the player's book; the book auto-equips into the
    /// first free slot). The UI never runs this logic — it only reads the resulting state.
    ///
    /// Reconcile runs on every mastery level change (notifying the player of freshly unlocked
    /// abilities) and once when the player is set (silent catch-up on the already-earned levels).
    /// The read handlers also call <see cref="Reconcile"/> defensively so the window is correct
    /// even if the ability data finished loading after those events fired.
    /// </summary>
    public class AbilityUnlockService : IDisposable, IAbilityUnlockService
    {
        private const string UnlockNotificationId = "Notification_Ability_Unlocked";

        private readonly IPlayerAccessor _playerAccessor;
        private readonly IAbilityProvider _abilityProvider;
        private readonly IMartialArtMastery _mastery;
        private readonly IGameMessageBus _messageBus;
        private readonly ILoadScope _loadScope;

        public AbilityUnlockService(
            IPlayerAccessor playerAccessor,
            IAbilityProvider abilityProvider,
            IMartialArtMastery mastery,
            IGameMessageBus messageBus,
            ILoadScope loadScope)
        {
            _playerAccessor = playerAccessor;
            _abilityProvider = abilityProvider;
            _mastery = mastery;
            _messageBus = messageBus;
            _loadScope = loadScope;

            _mastery.CurrentLevelChange += OnLevelChanged;
            _playerAccessor.PlayerChanged += OnPlayerChanged;
        }

        public void Dispose()
        {
            _mastery.CurrentLevelChange -= OnLevelChanged;
            _playerAccessor.PlayerChanged -= OnPlayerChanged;
        }

        /// <summary>Learns every ability whose threshold the player has reached but hasn't learned yet.</summary>
        public void Reconcile(bool notify)
        {
            var book = _playerAccessor.Player?.AbilityBook;
            if (book == null) return;

            var learnedIds = book.AllAbilities.Select(ability => ability.Id).ToHashSet();

            foreach (string abilityId in _abilityProvider.KnownAbilityIds)
            {
                if (learnedIds.Contains(abilityId)) continue;
                if (_abilityProvider.GetMasteryLevel(abilityId) > _mastery.CurrentLevel) continue;

                var ability = _abilityProvider.CreateAbility(abilityId);
                book.Learn(_abilityProvider.GetAbilityStance(abilityId), ability);
                // TODO:
                // what message and what was learned
                if (notify && !_loadScope.IsLoading) // restoring mastery re-learns silently
                {
                    _messageBus.PublishMessageAsync(new SendNotificationMessageMessage(UnlockNotificationId));
                }
            }
        }

        private void OnLevelChanged(int _) => Reconcile(notify: true);

        private void OnPlayerChanged(IPlayer _) => Reconcile(notify: false);
    }
}
