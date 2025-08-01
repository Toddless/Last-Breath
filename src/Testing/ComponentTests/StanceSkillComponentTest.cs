namespace LastBreathTest.ComponentTests
{
    using Contracts.Enums;
    using LastBreath.Script.Abilities.Interfaces;
    using LastBreath.Script.BattleSystem;
    using Moq;

    [TestClass]
    public class StanceSkillComponentTest
    {
        [TestMethod]
        public void RequirementsMetStanceSkillShouldBeActivated_Test()
        {
            bool isActivated = false;

            var stance = new Mock<IStance>();
            stance.Setup(x => x.StanceType).Returns(Stance.Dexterity);
            var stanceObject = stance.Object;

            var stanceSkill = new Mock<IStanceSkill>();
            stanceSkill.Setup(x => x.RequiredStance).Returns(Stance.Dexterity);
            stanceSkill.Setup(x => x.Type).Returns(SkillType.AlwaysActive);
            stanceSkill.Setup(x => x.Activate(stanceObject)).Callback(new InvocationAction(invocation =>
            {
                isActivated = true;
            }));

            var stanceComponent = new StanceSkillComponent(stanceObject);

            stanceComponent.AddSkill(stanceSkill.Object);

            Assert.IsTrue(isActivated);
        }

        [TestMethod]
        public void RequirementsNotMetStanceSkillShouldBeNotActivated_Test()
        {
            bool isActivated = false;

            var stance = new Mock<IStance>();
            stance.Setup(x => x.StanceType).Returns(Stance.Dexterity);
            var stanceObject = stance.Object;

            var stanceSkill = new Mock<IStanceSkill>();
            stanceSkill.Setup(x => x.Type).Returns(SkillType.AlwaysActive);
            stanceSkill.Setup(x => x.RequiredStance).Returns(Stance.Strength);
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
