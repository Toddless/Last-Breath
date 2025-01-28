namespace PlaygroundTest
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using Playground;
    using Playground.Components.EffectTypeHandlers;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Passives.Attacks;

    public class PlayerTest : ICharacter
    {
        private HealthComponent? _healthComponent;
        private AttackComponent? _attackComponent;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private ObservableCollection<IEffect> _effects;

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

        public ObservableCollection<IEffect> Effects
        {
            get => _effects;
            private set => _effects = value;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable. Only for test class
        public PlayerTest()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            Effects = [];
            HealthComponent = new(Effects, new EffectHandlerFactory());
            AttackComponent = new(Effects, new EffectHandlerFactory());
            AppliedAbilities ??= [];
            // Player class will be never deleted or disposed so i don`t care about unsubscribe here
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
