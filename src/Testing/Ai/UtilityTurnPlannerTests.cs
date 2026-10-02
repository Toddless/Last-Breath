namespace LastBreathTest.Ai
{
    using Core.Ai;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Moq;

    [TestClass]
    public class UtilityTurnPlannerTests
    {
        [TestMethod]
        public async Task CastsWorthwhileAbilityThenClosesWithBasicAttack()
        {
            var enemy = CreateFighter().Object;
            var self = CreateSelf(CreateAbility("Ability_A").Object);
            var env = CreateEnvironment(self.Object, enemy);

            await CreatePlanner().PlayTurnAsync(self.Object, CreateProfile(("Ability_A", 1f, AbilityRole.Damage)), env.Object);

            env.Verify(e => e.CastAbilityAsync(self.Object, It.Is<IAbility>(a => a.Id == "Ability_A"), It.IsAny<IReadOnlyList<IFightable>>()), Times.Once);
            env.Verify(e => e.BasicAttackAsync(self.Object, enemy), Times.Once);
        }

        [TestMethod]
        public async Task RespectsMaxCastsPerTurn()
        {
            var enemy = CreateFighter().Object;
            var self = CreateSelf(CreateAbility("Ability_A").Object, CreateAbility("Ability_B").Object);
            var env = CreateEnvironment(self.Object, enemy);
            var profile = CreateProfile(maxCasts: 1, ("Ability_A", 1f, AbilityRole.Damage), ("Ability_B", 1f, AbilityRole.Damage));

            await CreatePlanner().PlayTurnAsync(self.Object, profile, env.Object);

            env.Verify(e => e.CastAbilityAsync(It.IsAny<IFightable>(), It.IsAny<IAbility>(), It.IsAny<IReadOnlyList<IFightable>>()), Times.Once);
            env.Verify(e => e.BasicAttackAsync(self.Object, enemy), Times.Once);
        }

        [TestMethod]
        public async Task SimpleIntellectIsClampedToOneCast()
        {
            var enemy = CreateFighter().Object;
            var self = CreateSelf(CreateAbility("Ability_A").Object, CreateAbility("Ability_B").Object);
            var env = CreateEnvironment(self.Object, enemy);
            var profile = CreateProfile(maxCasts: 3, intellect: AiIntellect.Simple,
                ("Ability_A", 1f, AbilityRole.Damage), ("Ability_B", 1f, AbilityRole.Damage));

            await CreatePlanner().PlayTurnAsync(self.Object, profile, env.Object);

            env.Verify(e => e.CastAbilityAsync(It.IsAny<IFightable>(), It.IsAny<IAbility>(), It.IsAny<IReadOnlyList<IFightable>>()), Times.Once);
        }

        [TestMethod]
        public async Task UnaffordableAbilityIsSkippedButAttackStillHappens()
        {
            var enemy = CreateFighter().Object;
            var self = CreateSelf(CreateAbility("Ability_A", canActivate: false).Object);
            var env = CreateEnvironment(self.Object, enemy);

            await CreatePlanner().PlayTurnAsync(self.Object, CreateProfile(("Ability_A", 1f, AbilityRole.Damage)), env.Object);

            env.Verify(e => e.CastAbilityAsync(It.IsAny<IFightable>(), It.IsAny<IAbility>(), It.IsAny<IReadOnlyList<IFightable>>()), Times.Never);
            env.Verify(e => e.BasicAttackAsync(self.Object, enemy), Times.Once);
        }

        [TestMethod]
        public async Task LowWeightBelowThresholdIsNotCast()
        {
            var enemy = CreateFighter().Object;
            var self = CreateSelf(CreateAbility("Ability_A").Object);
            var env = CreateEnvironment(self.Object, enemy);

            await CreatePlanner().PlayTurnAsync(self.Object, CreateProfile(("Ability_A", 0.1f, AbilityRole.Damage)), env.Object);

            env.Verify(e => e.CastAbilityAsync(It.IsAny<IFightable>(), It.IsAny<IAbility>(), It.IsAny<IReadOnlyList<IFightable>>()), Times.Never);
            env.Verify(e => e.BasicAttackAsync(self.Object, enemy), Times.Once);
        }

        [TestMethod]
        public async Task DamageCastAndAttackFocusTheDyingEnemy()
        {
            var healthy = CreateFighter(health: 1000).Object;
            var dying = CreateFighter(health: 100).Object;
            var self = CreateSelf(CreateAbility("Ability_A").Object);
            var env = CreateEnvironment(self.Object, healthy, dying);

            await CreatePlanner().PlayTurnAsync(self.Object, CreateProfile(("Ability_A", 1f, AbilityRole.Damage)), env.Object);

            env.Verify(e => e.CastAbilityAsync(self.Object, It.IsAny<IAbility>(),
                It.Is<IReadOnlyList<IFightable>>(targets => targets.Count == 1 && targets[0] == dying)), Times.Once);
            env.Verify(e => e.BasicAttackAsync(self.Object, dying), Times.Once);
        }

        [TestMethod]
        public async Task NoLivingEnemiesMeansNoActions()
        {
            var self = CreateSelf(CreateAbility("Ability_A").Object);
            var env = CreateEnvironment(self.Object); // empty field

            await CreatePlanner().PlayTurnAsync(self.Object, CreateProfile(("Ability_A", 1f, AbilityRole.Damage)), env.Object);

            env.Verify(e => e.CastAbilityAsync(It.IsAny<IFightable>(), It.IsAny<IAbility>(), It.IsAny<IReadOnlyList<IFightable>>()), Times.Never);
            env.Verify(e => e.BasicAttackAsync(It.IsAny<IFightable>(), It.IsAny<IFightable>()), Times.Never);
        }

        [TestMethod]
        public async Task ChargedAbilityTakesGreedScaledStage()
        {
            var enemy = CreateFighter().Object;
            var charged = CreateAbility("Ability_A").As<IChargedAbility>();
            charged.SetupGet(c => c.MaxStage).Returns(3);
            charged.SetupGet(c => c.MaxAffordableStage).Returns(3);
            charged.SetupProperty(c => c.PendingStage);
            var self = CreateSelf((IAbility)charged.Object);
            var env = CreateEnvironment(self.Object, enemy);

            await CreatePlanner().PlayTurnAsync(self.Object, CreateProfile(greed: 0.5f, ("Ability_A", 1f, AbilityRole.Damage)), env.Object);

            Assert.AreEqual(2, charged.Object.PendingStage); // round(3 * 0.5) = 2
        }

        [TestMethod]
        public async Task BrokenMoraleFleesInsteadOfPlayingTheTurn()
        {
            var enemy = CreateFighter().Object;
            var self = CreateSelf(CreateAbility("Ability_A").Object);
            self.SetupGet(f => f.CurrentHealth).Returns(100f); // 10% of the 1000 max
            var env = CreateEnvironment(self.Object, enemy);
            var profile = new BehaviorProfile
            {
                Id = "Behavior_Coward",
                Temperature = 0f,
                FleeHealthThreshold = 0.15f,
                Abilities = new Dictionary<string, AbilityBehavior> { ["Ability_A"] = new("Ability_A", 1f, AbilityRole.Damage) },
            };

            await CreatePlanner().PlayTurnAsync(self.Object, profile, env.Object);

            env.Verify(e => e.FleeBattleAsync(self.Object), Times.Once);
            env.Verify(e => e.CastAbilityAsync(It.IsAny<IFightable>(), It.IsAny<IAbility>(), It.IsAny<IReadOnlyList<IFightable>>()), Times.Never);
            env.Verify(e => e.BasicAttackAsync(It.IsAny<IFightable>(), It.IsAny<IFightable>()), Times.Never);
        }

        [TestMethod]
        public async Task FearlessProfileNeverFlees()
        {
            var enemy = CreateFighter().Object;
            var self = CreateSelf(CreateAbility("Ability_A").Object);
            self.SetupGet(f => f.CurrentHealth).Returns(100f);
            var env = CreateEnvironment(self.Object, enemy);

            await CreatePlanner().PlayTurnAsync(self.Object, CreateProfile(("Ability_A", 1f, AbilityRole.Damage)), env.Object);

            env.Verify(e => e.FleeBattleAsync(It.IsAny<IFightable>()), Times.Never);
            env.Verify(e => e.BasicAttackAsync(self.Object, enemy), Times.Once);
        }

        // ---- helpers ----

        private static UtilityTurnPlanner CreatePlanner() => new(new DefaultRandomNumberGenerator(seed: 42));

        private static BehaviorProfile CreateProfile(params (string Id, float Weight, AbilityRole Role)[] abilities) =>
            CreateProfile(maxCasts: 2, AiIntellect.Tactical, 1f, abilities);

        private static BehaviorProfile CreateProfile(int maxCasts, params (string Id, float Weight, AbilityRole Role)[] abilities) =>
            CreateProfile(maxCasts, AiIntellect.Tactical, 1f, abilities);

        private static BehaviorProfile CreateProfile(int maxCasts, AiIntellect intellect, params (string Id, float Weight, AbilityRole Role)[] abilities) =>
            CreateProfile(maxCasts, intellect, 1f, abilities);

        private static BehaviorProfile CreateProfile(float greed, params (string Id, float Weight, AbilityRole Role)[] abilities) =>
            CreateProfile(maxCasts: 2, AiIntellect.Tactical, greed, abilities);

        private static BehaviorProfile CreateProfile(int maxCasts, AiIntellect intellect, float greed, (string Id, float Weight, AbilityRole Role)[] abilities) => new()
        {
            Id = "Behavior_Test",
            Intellect = intellect,
            MaxCastsPerTurn = maxCasts,
            Temperature = 0f, // deterministic scoring in tests
            Greed = greed,
            Abilities = abilities.ToDictionary(entry => entry.Id, entry => new AbilityBehavior(entry.Id, entry.Weight, entry.Role)),
        };

        private static Mock<IFightable> CreateFighter(float maxHealth = 1000f, float health = 1000f, float damage = 100f)
        {
            var parameters = new Mock<IEntityParametersComponent>();
            parameters.SetupGet(p => p.MaxHealth).Returns(maxHealth);
            parameters.SetupGet(p => p.MaxMana).Returns(500f);
            parameters.SetupGet(p => p.PhysicalDamage).Returns(damage);

            var fighter = new Mock<IFightable>();
            fighter.SetupGet(f => f.Parameters).Returns(parameters.Object);
            fighter.SetupGet(f => f.CurrentHealth).Returns(health);
            fighter.SetupGet(f => f.CurrentMana).Returns(500f);
            fighter.SetupGet(f => f.IsAlive).Returns(true);
            return fighter;
        }

        private static Mock<IFightable> CreateSelf(params IAbility[] abilities)
        {
            var book = new Mock<IAbilityBookComponent>();
            book.SetupGet(b => b.ActiveAbilities).Returns(abilities.ToList());

            var self = CreateFighter();
            self.SetupGet(f => f.AbilityBook).Returns(book.Object);
            return self;
        }

        /// <summary>Targeting requires manual selection and offers every enemy — the planner must rank and pick.</summary>
        private static Mock<IAbility> CreateAbility(string id, bool canActivate = true)
        {
            var targeting = new Mock<ITargetingStrategy>();
            targeting.SetupGet(t => t.RequiresManualSelection).Returns(true);
            targeting.SetupGet(t => t.MaxTargets).Returns(1);
            targeting.Setup(t => t.GetValidTargets(It.IsAny<IFightable>(), It.IsAny<IBattleField>()))
                .Returns((IFightable caster, IBattleField field) => field.GetEnemies(caster));

            var ability = new Mock<IAbility>();
            ability.SetupGet(a => a.Id).Returns(id);
            // Single-use: the second availability check reports false, simulating the cooldown
            // a real ability enters after Execute (mocks never go on cooldown by themselves).
            ability.SetupSequence(a => a.CanActivate()).Returns(canActivate).Returns(false);
            ability.SetupGet(a => a.CostType).Returns(Costs.Mana);
            ability.SetupGet(a => a.CostValue).Returns(50);
            ability.SetupGet(a => a.Targeting).Returns(targeting.Object);
            return ability;
        }

        private static Mock<ICombatEnvironment> CreateEnvironment(IFightable self, params IFightable[] enemies)
        {
            var field = new Mock<IBattleField>();
            field.Setup(f => f.GetEnemies(It.IsAny<IFightable>())).Returns(enemies.ToList());
            field.Setup(f => f.GetAllies(It.IsAny<IFightable>())).Returns(new List<IFightable>());

            var env = new Mock<ICombatEnvironment>();
            env.SetupGet(e => e.Field).Returns(field.Object);
            env.Setup(e => e.CastAbilityAsync(It.IsAny<IFightable>(), It.IsAny<IAbility>(), It.IsAny<IReadOnlyList<IFightable>>()))
                .Returns(Task.CompletedTask);
            env.Setup(e => e.BasicAttackAsync(It.IsAny<IFightable>(), It.IsAny<IFightable>()))
                .Returns(Task.CompletedTask);
            env.Setup(e => e.FleeBattleAsync(It.IsAny<IFightable>()))
                .Returns(Task.CompletedTask);
            return env;
        }
    }
}
