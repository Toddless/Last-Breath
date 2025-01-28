namespace PlaygroundTest
{
    using Playground.Script.Effects.Interfaces;
    using Playground.Script.Passives;
    using Playground.Script.Passives.Buffs;
    using Playground.Script.Passives.Debuffs;

    [TestClass]
    public class AbilityTest
    {
        private PlayerTest? _player;
        // critical strike chance and critical damage
        private TestBuffAbility? _buff;
        // health and damage
        private TestDebuffAbility? _debuff;


        [TestInitialize]
        public void Initialize()
        {
            _player = new PlayerTest();
            _buff = new TestBuffAbility();
            _debuff = new TestDebuffAbility();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _player?.AttackComponent?.Dispose();
            _player?.HealthComponent?.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        [TestMethod]
        public void AddingAbilityInCollectionTest()
        {
            Assert.IsNotNull(_player);
            _buff?.ActivateAbility(_player);
            Assert.IsTrue(_player.AppliedAbilities?.Count > 0);
        }

        [TestMethod]
        public void AddedAbilitySetEffectsTest()
        {
            Assert.IsNotNull(_player);
            var health = _player.HealthComponent?.CurrentHealth;
            var damage = _player.AttackComponent?.CurrentMinDamage;

            _debuff?.ActivateAbility(_player);

            Assert.IsTrue(_player.HealthComponent?.CurrentHealth < health);
            Assert.IsTrue(_player.AttackComponent?.CurrentMinDamage < damage);
        }


        [TestMethod]
        [DynamicData(nameof(GetAbilities), DynamicDataSourceType.Method)]
        public void AllAbilitiesEffectAppliedTest(List<IAbility> abilities)
        {
            var player = new PlayerTest();
            foreach (var item in abilities)
            {
                item.ActivateAbility(player);

            }
            Assert.IsTrue(player.Effects?.Count > 3);
        }


        private static IEnumerable<object[]> GetAbilities()
        {
            yield return new object[] {
                 new List<IAbility>
                    {
                        new TestBuffAbility
                        {
                            OnReceiveAbilityHandler = AbilityHandler.ApplyAbility,
                            Effects = new List<IEffect>
                            {
                                new CriticalStrikeChanceBuff(string.Empty, string.Empty, 0.1f, 3),
                                new StrikeDamageBuff(string.Empty, string.Empty, 0.1f, 3)
                            }
                        },
                        new TestDebuffAbility
                        {
                            OnReceiveAbilityHandler = AbilityHandler.ApplyAbility,
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
