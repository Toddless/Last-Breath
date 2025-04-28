namespace PlaygroundTest
{
    using System.Collections.ObjectModel;
    using Playground.Components;
    using Playground.Script;
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Enums;

    public class BaseEnemyTest : ICharacter
    {
        private HealthComponent? _healthComponent;
        private DamageComponent? _attackComponent;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private ObservableCollection<IEffect> _appliedEffects;
        private List<IAbility> _abilities;


        public HealthComponent? Health { get => _healthComponent; }
        public DamageComponent? Damage { get => _attackComponent; }
        public ObservableCollection<IAbility>? AppliedAbilities { get => _appliedAbilities; set => _appliedAbilities = value; }

        public List<IAbility> Abilities { get => _abilities; }

        public DefenseComponent? Defense => throw new NotImplementedException();

        public Stance Stance { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public BaseEnemyTest()
        {
            _appliedAbilities = [];
            _appliedEffects = [];
            AppliedAbilities = [];
        }
    }
}
