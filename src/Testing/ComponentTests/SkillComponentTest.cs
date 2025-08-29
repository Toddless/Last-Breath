namespace LastBreathTest.ComponentTests
{
    using Core.Enums;
    using Core.Interfaces.Skills;
    using LastBreath.Components;
    using LastBreath.Script;
    using LastBreath.Script.Abilities.Interfaces;
    using Moq;

    [TestClass]
    public class SkillComponentTest
    {
        [TestMethod]
        public void TargetSkillActivated_Test()
        {
            bool activated = false;
            var character = new Mock<ICharacter>().Object;
            var skill = new Mock<ITargetSkill>();
            skill.Setup(x => x.Type).Returns(SkillType.AlwaysActive);
            skill.Setup(x => x.Activate(character)).Callback(new InvocationAction(invocation =>
            {
                activated = true;
            }));

            var skillComp = new SkillsComponent(character);
            skillComp.AddSkill(skill.Object);

            Assert.IsTrue(activated);
        }


        [TestMethod]
        public void TargetSkillNotActivated_Test()
        {
            bool activated = false;
            var character = new Mock<ICharacter>().Object;

            var skill = new Mock<ITargetSkill>();
            skill.Setup(x => x.Type).Returns(SkillType.PreAttack);
            skill.Setup(x => x.Activate(character)).Callback(new InvocationAction(invocation =>
            {
                activated = true;
            }));

            var skillComp = new SkillsComponent(character);
            skillComp.AddSkill(skill.Object);

            Assert.IsFalse(activated);
        }

        [TestMethod]
        public void SameSkillWillNotBeAddedTwice_Test()
        {
            var character = new Mock<ICharacter>().Object;
            var skill = new Mock<ISkill>();
            skill.Setup(x => x.Type).Returns(SkillType.PreAttack);

            var skillComp = new SkillsComponent(character);
            skillComp.AddSkill(skill.Object);
            skillComp.AddSkill(skill.Object);

            var count = skillComp.GetSkills(SkillType.PreAttack).Count;

            Assert.IsTrue(count == 1);
        }
    }
}
