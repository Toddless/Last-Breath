namespace PlaygroundTest
{
    using System.Collections.ObjectModel;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enums;

    public class PlayerTest : ICharacter
    {
        private HealthComponent? _healthComponent;
        private DamageComponent? _attackComponent;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private ObservableCollection<IEffect> _effects;
        private List<IAbility> _abilities;

        public HealthComponent? Health
        {
            get => _healthComponent;
        }
        public DamageComponent? Damage
        {
            get => _attackComponent;
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

        public List<IAbility> Abilities
        {
            get => _abilities;
        }

        public DefenseComponent? Defense => throw new NotImplementedException();

        public Stance Stance { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        // Only for test class
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable. 
        public PlayerTest()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            Effects = [];
            AppliedAbilities ??= [];
        }
    }
}
