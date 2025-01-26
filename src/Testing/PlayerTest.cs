namespace PlaygroundTest
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using Playground;
    using Playground.Script.Passives;
    using Playground.Script.Passives.Attacks;

    public class PlayerTest : ICharacter
    {
        private HealthComponent? _healthComponent;
        private AttackComponent? _attackComponent;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private ObservableCollection<IEffect>? _effects;

        public HealthComponent? HealthComponent
        {
            get => _healthComponent;
            set => _healthComponent = value;
        }
        public AttackComponent? AttackComponent
        {
            get => _attackComponent;
            set => _attackComponent = value;
        }
        public ObservableCollection<IAbility>? AppliedAbilities
        {
            get => _appliedAbilities;
            set => _appliedAbilities = value;
        }

        public ObservableCollection<IEffect>? Effects
        {
            get => _effects;
            private set => _effects = value;
        }

        public PlayerTest()
        {
            Effects ??= [];
            HealthComponent = new(Effects);
            AttackComponent = new(Effects);
            AppliedAbilities ??= [];
            AppliedAbilities.CollectionChanged += OnAddAbility;
        }

        private void OnAddAbility(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add && e.Action != NotifyCollectionChangedAction.Remove)
                return;
            if (e.OldItems != null)
            {
                RemoveOldAbilitiesEffects(e.OldItems.OfType<IAbility>());
            }
            if (e.NewItems != null)
            {
                AddNewAbilitiesEffects(e.NewItems.OfType<IAbility>());
            }

        }

        private void AddNewAbilitiesEffects(IEnumerable<IAbility> abilities)
        {
            foreach (var effect in abilities.SelectMany(ability => ability.Effects))
            {
                Effects?.Add(effect);
            }
        }

        private void RemoveOldAbilitiesEffects(IEnumerable<IAbility> abilities)
        {
            foreach (var effect in abilities.SelectMany(ability => ability.Effects))
            {
                Effects?.Remove(effect);
            }
        }
    }
}
