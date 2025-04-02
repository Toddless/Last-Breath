namespace PlaygroundTest
{
    using System.Collections.ObjectModel;
    using Playground;
    using Playground.Components;
    using Playground.Script.Effects.Interfaces;

    public class BaseEnemyTest : ICharacter
    {
        private HealthComponent? _healthComponent;
        private DamageComponent? _attackComponent;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private ObservableCollection<IEffect> _appliedEffects;
        private List<IAbility> _abilities;


        public HealthComponent? EnemyHealth { get => _healthComponent; set => _healthComponent = value; }
        public DamageComponent? EnemyDamage { get => _attackComponent; set => _attackComponent = value; }
        public ObservableCollection<IAbility>? AppliedAbilities { get => _appliedAbilities; set => _appliedAbilities = value; }

        public List<IAbility> Abilities { get => _abilities; }

        public BaseEnemyTest()
        {
            _appliedAbilities = [];
            _appliedEffects = [];
            AppliedAbilities = [];
        }
    }
}
