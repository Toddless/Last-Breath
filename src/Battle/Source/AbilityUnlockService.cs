namespace Battle.Source
{
    using System;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Save;
    using Core.Services;

    /// <summary>
    /// Keeps the player's ability book in sync with the ability catalog: every non-hidden ability the
    /// book does not hold yet is learned into it (the book auto-equips into the first free slot).
    /// The UI never runs this logic — it only reads the resulting state.
    ///
    /// Reconcile runs once when the player is set (silent catch-up) and defensively from the read
    /// handlers, so a window is correct even if the ability data finished loading after that event.
    /// </summary>
    public class AbilityUnlockService : IDisposable, IAbilityUnlockService
    {
        private const string UnlockNotificationId = "Notification_Ability_Unlocked";

        private readonly IPlayerAccessor _playerAccessor;
        private readonly IAbilityProvider _abilityProvider;
        private readonly IGameMessageBus _messageBus;
        private readonly ILoadScope _loadScope;

        public AbilityUnlockService(
            IPlayerAccessor playerAccessor,
            IAbilityProvider abilityProvider,
            IGameMessageBus messageBus,
            ILoadScope loadScope)
        {
            _playerAccessor = playerAccessor;
            _abilityProvider = abilityProvider;
            _messageBus = messageBus;
            _loadScope = loadScope;

            _playerAccessor.PlayerChanged += OnPlayerChanged;

            // Children run _Ready before their parent: the player registers with the accessor
            // before Main._Ready first resolves this service, so PlayerChanged has already fired.
            if (_playerAccessor.Player != null) Reconcile(notify: false);
        }

        public void Dispose() => _playerAccessor.PlayerChanged -= OnPlayerChanged;

        /// <summary>Learns every non-hidden ability the player's book does not hold yet.</summary>
        public void Reconcile(bool notify)
        {
            var book = _playerAccessor.Player?.AbilityBook;
            if (book == null) return;

            var learnedIds = book.AllAbilities.Select(ability => ability.Id).ToHashSet();

            foreach (string abilityId in _abilityProvider.KnownAbilityIds)
            {
                if (_abilityProvider.IsHidden(abilityId)) continue; // boss reactions are not for the player's book
                if (learnedIds.Contains(abilityId)) continue;

                var ability = _abilityProvider.CreateAbility(abilityId);
                book.Learn(_abilityProvider.GetAbilityStance(abilityId), ability);
                // TODO:
                // what message and what was learned
                if (notify && !_loadScope.IsLoading) // a restored book re-learns silently
                {
                    _messageBus.PublishMessageAsync(new SendNotificationMessageMessage(UnlockNotificationId));
                }
            }
        }

        private void OnPlayerChanged(IPlayer _) => Reconcile(notify: false);
    }
}
