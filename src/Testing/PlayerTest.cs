namespace PlaygroundTest
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using Playground;
    using Playground.Components;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Passives.Attacks;

    public class PlayerTest : ICharacter
    {
        private HealthComponent? _healthComponent;
        private AttackComponent? _attackComponent;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private ObservableCollection<IEffect> _effects;
        private EffectManager? _effectManager;

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
        // Only for test class
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable. 
        public PlayerTest()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            Effects = [];
            AppliedAbilities ??= [];
            _effectManager = new(Effects);
            HealthComponent = new(_effectManager.CalculateValues);
            AttackComponent = new(_effectManager.CalculateValues);
            AppliedAbilities.CollectionChanged += _effectManager.OnAddAbility;
            _effectManager.TakeDamage += HealthComponent.TakeDamage;
            _effectManager.Heal += HealthComponent.Heal;
            _effectManager.UpdateProperties += HealthComponent.UpdateProperties;
            _effectManager.UpdateProperties += AttackComponent.UpdateProperties;
        }
    }
}
