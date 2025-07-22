namespace PlaygroundTest.ComponentTests
{
    using Moq;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.BattleSystem;

    [TestClass]
    public class StanceSkillComponentTest
    {
        [TestMethod]
        public void StanceSkillActivated_Test()
        {
            bool isActivated = false;

            var stance = new Mock<IStance>();
            stance.Setup(x => x.StanceType).Returns(Playground.Script.Enums.Stance.Dexterity);
            var stanceObject = stance.Object;

            var stanceSkill = new Mock<IStanceSkill>();
            stanceSkill.Setup(x => x.RequiredStance).Returns(Playground.Script.Enums.Stance.Dexterity);
            stanceSkill.Setup(x => x.Activate(stanceObject)).Callback(new InvocationAction(invocation =>
            {
                isActivated = true;
            }));

            var stanceComponent = new StanceSkillComponent(stanceObject);

            stanceComponent.AddSkill(stanceSkill.Object);

            Assert.IsTrue(isActivated);
        }

        [TestMethod]
        public void StanceSkillNotActivated_Test()
        {
            bool isActivated = false;

            var stance = new Mock<IStance>();
            stance.Setup(x => x.StanceType).Returns(Playground.Script.Enums.Stance.Dexterity);
            var stanceObject = stance.Object;

            var stanceSkill = new Mock<IStanceSkill>();
            stanceSkill.Setup(x => x.RequiredStance).Returns(Playground.Script.Enums.Stance.Strength);
            stanceSkill.Setup(x => x.Activate(stanceObject)).Callback(new InvocationAction(invocation =>
            {
                isActivated = true;
            }));

            var stanceComponent = new StanceSkillComponent(stanceObject);

            stanceComponent.AddSkill(stanceSkill.Object);

            Assert.IsFalse(isActivated);
        }
    }
}
