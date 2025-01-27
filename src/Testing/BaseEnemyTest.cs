namespace PlaygroundTest
{
    using Playground;
    using Playground.Script.Passives.Attacks;
    using System.Collections.ObjectModel;
    using Playground.Script.Effects.Interfaces;

    public class BaseEnemyTest : ICharacter
    {
        private HealthComponent? _healthComponent;
        private AttackComponent? _attackComponent;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private ObservableCollection<IEffect> _appliedEffects;


        public HealthComponent? HealthComponent { get => _healthComponent; set => _healthComponent = value; }
        public AttackComponent? AttackComponent { get => _attackComponent; set => _attackComponent = value; }
        public ObservableCollection<IAbility>? AppliedAbilities { get => _appliedAbilities; set => _appliedAbilities = value; }

        public BaseEnemyTest()
        {
            _appliedAbilities = [];
            HealthComponent = new(_appliedEffects);
            AttackComponent = new(_appliedEffects);
            AppliedAbilities = [];
        }
    }
}
