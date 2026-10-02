namespace LastBreathTest.Combat
{
    using System.Threading.Tasks;
    using Battle.Source.Abilities;
    using Battle.Source.Abilities.Riders;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Block as the design writes it: "заблокированный удар не наносит урон и не накладывает эффекты.
    /// Базовый шанс 5%". The roll itself and its place in the landing order are claimed by
    /// <see cref="ChanceRollTests"/>; claimed here is everything that made the stat a line on gear and
    /// nothing else — the baseline every fighter is born with, the ceiling that keeps a stacked build
    /// touchable, the percent lines having a base to amplify, and a blocked swing being no impact at all
    /// so nothing rides along on it.
    /// </summary>
    [TestClass]
    public class BlockTests
    {
        private const float DocumentedBase = 0.05f;

        [TestMethod]
        public void TheShippedBaselineCarriesTheDocumentedBlockChance()
        {
            JToken? shipped = Unarmed()[nameof(EntityParameter.BlockChance)];

            Assert.IsNotNull(shipped, "the unarmed profile names no block chance — the documented 5% baseline is gone");
            Assert.AreEqual(DocumentedBase, shipped.Value<float>(), 0.0001f,
                "the shipped baseline has drifted from the documented base block chance");
        }

        [TestMethod]
        public void PercentBlockLinesAmplifyTheBaselineWithNothingFlatWorn()
        {
            // Without a baseline an increase-only ring resolved to zero and its line was a lie.
            var parameters = Fighter(new SimpleModifier(EntityParameter.BlockChance, ModifierValueType.Increase, 0.4f, "ring"));

            Assert.AreEqual(DocumentedBase * 1.4f, parameters.BlockChance, 0.0001f);
        }

        [TestMethod]
        public void BlockIsCappedSoNoBuildBecomesUntouchable()
        {
            var parameters = Fighter(new SimpleModifier(EntityParameter.BlockChance, ModifierValueType.Flat, 5f, "gear"));

            Assert.AreEqual(0.9f, parameters.BlockChance, 0.0001f,
                "a fighter stacked past the ceiling must still be reachable");
        }

        [TestMethod]
        public async Task ABlockedSwingIsNoImpactAndLaysNothing()
        {
            var effect = new Mock<IEffect>();
            effect.Setup(laid => laid.Copy()).Returns(effect.Object);
            var rider = new ApplyEffectImpactRider(effect.Object);

            AbilityImpact blocked = Impact(AttackResults.Blocked);
            Assert.IsFalse(blocked.Succeeded, "a blocked swing reached the riders as a landed one");

            await rider.Apply(blocked);
            effect.Verify(laid => laid.Apply(It.IsAny<EffectApplyingContext>()), Times.Never(),
                "a blocked attack laid its effect anyway");

            await rider.Apply(Impact(AttackResults.Succeed));
            effect.Verify(laid => laid.Apply(It.IsAny<EffectApplyingContext>()), Times.Once(),
                "the same rider lays nothing on a landed attack either — the claim above proves nothing");
        }

        private static AbilityImpact Impact(AttackResults result)
        {
            var fighter = Mock.Of<IFightable>();
            var context = new Mock<IAttackContext>();
            context.SetupGet(attack => attack.Result).Returns(result);
            context.SetupGet(attack => attack.Attacker).Returns(fighter);
            context.SetupGet(attack => attack.Target).Returns(fighter);

            return context.Object.ToImpact(Mock.Of<IBattleField>(), Mock.Of<IAbility>());
        }

        private static EntityParametersComponent Fighter(IModifier worn)
        {
            var parameters = new EntityParametersComponent();
            parameters.Initialize(_ => [worn]);
            parameters.SetBaseValueForParameter(EntityParameter.BlockChance, DocumentedBase);
            return parameters;
        }

        /// <summary>The unarmed profile straight off the shipped file: what is claimed is that the FILE
        /// carries the documented baseline, and a reading through the provider would add a second thing
        /// that could be wrong.</summary>
        private static JObject Unarmed()
        {
            string path = Path.Combine(SharedData.Catalog(DataCatalog.PlayerStats), "PlayerStats.json");
            Assert.IsTrue(File.Exists(path), $"the shipped player stats are missing at {path}");

            var profile = JObject.Parse(File.ReadAllText(path))["unarmed"] as JObject;
            Assert.IsNotNull(profile, "the shipped player stats carry no 'unarmed' profile");
            return profile;
        }
    }
}
