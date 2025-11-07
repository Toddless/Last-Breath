namespace LastBreathTest.BattleSystemTests
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;
    using Moq;

    [TestClass]
    public class StanceTest
    {

        private DexterityStanceTest CreateDexterity()
        {
            var character = new Mock<IEntity>();
            var resource = new Mock<IResource>();
            var activationEffect = new Mock<IStanceActivationEffect>();

            return new(character.Object, resource.Object, activationEffect.Object, Stance.Dexterity);
        }
    }
}
