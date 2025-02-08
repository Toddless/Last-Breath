namespace PlaygroundTest
{
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Passives;
    using Playground.Script.Passives.Buffs;
    using Playground.Script.Passives.Debuffs;
    using Playground.Script.Scenes;

    [TestClass]
    public class AbilityTest
    {
        private PlayerTest? _player;
        // critical strike chance and critical damage
        private TestBuffAbility? _buff;
        // health and damage
        private TestDebuffAbility? _debuff;
        private IBattleContext _battleContext;


        [TestInitialize]
        public void Initialize()
        {
            _player = new PlayerTest();
            _buff = new TestBuffAbility();
            _debuff = new TestDebuffAbility();
            _battleContext = new BattleContext(_player, new BaseEnemyTest());
        }

        [TestMethod]
        public void AddingAbilityInCollectionTest()
        {
            Assert.IsNotNull(_player);
            _buff?.ActivateAbility(_battleContext);
            Assert.IsTrue(_player.AppliedAbilities?.Count > 0);
        }

        [TestMethod]
        public void AddedAbilitySetEffectsTest()
        {
            Assert.IsNotNull(_player);
            var health = _player.HealthComponent?.CurrentHealth;
            var damage = _player.AttackComponent?.CurrentMinDamage;

            _debuff?.ActivateAbility(_battleContext);

            Assert.IsTrue(_player.HealthComponent?.CurrentHealth < health);
            Assert.IsTrue(_player.AttackComponent?.CurrentMinDamage < damage);
        }

        private static IEnumerable<object[]> GetAbilities()
        {
            yield return new object[] {
                 new List<IAbility>
                    {
                        new TestBuffAbility
                        {
                            AbilityHandler = AbilityHandler.ApplyAbility,
                            Effects = new List<IEffect>
                            {
                                new CriticalStrikeChanceBuff(string.Empty, string.Empty, 0.1f, 3),
                                new StrikeDamageBuff(string.Empty, string.Empty, 0.1f, 3)
                            }
                        },
                        new TestDebuffAbility
                        {
                            AbilityHandler = AbilityHandler.ApplyAbility,
                            Effects = new List<IEffect>
                            {
                                new HealthDebuf(string.Empty, string.Empty, -0.1f, 3),
                                new CriticalStrikeDamageDebuff(string.Empty, string.Empty, -0.1f, 3)
                            }
                        }
                    }
            };
        }
    };
}
