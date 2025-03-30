namespace PlaygroundTest
{
    using System.Collections.ObjectModel;
    using Playground;
    using Playground.Components;
    using Playground.Script.Effects.Interfaces;

    public class BaseEnemyTest : ICharacter
    {
        private HealthComponent? _healthComponent;
        private AttackComponent? _attackComponent;
        private ObservableCollection<IAbility>? _appliedAbilities;
        private ObservableCollection<IEffect> _appliedEffects;
        private EffectManager _effectManager;
        private List<IAbility> _abilities;


        public HealthComponent? HealthComponent { get => _healthComponent; set => _healthComponent = value; }
        public AttackComponent? AttackComponent { get => _attackComponent; set => _attackComponent = value; }
        public ObservableCollection<IAbility>? AppliedAbilities { get => _appliedAbilities; set => _appliedAbilities = value; }

        public List<IAbility> Abilities { get => _abilities; }

        public EffectManager EffectManager { get => _effectManager; }

        public BaseEnemyTest()
        {
            _effectManager = new([]);
            _appliedAbilities = [];
            _appliedEffects = [];
            HealthComponent = new(_effectManager.CalculateValues);
            AppliedAbilities = [];
        }
    }
}
