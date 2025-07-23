namespace PlaygroundTest.BattleSystemTests
{
    using Moq;
    using Playground.Components.Interfaces;
    using Playground.Script;
    using Playground.Script.BattleSystem;
    using Playground.Script.Enums;

    [TestClass]
    public class StanceTest
    {
        public void FirstTest()
        {
            var stance = CreateDexterity();
        }


        private DexterityStanceTest CreateDexterity()
        {
            var character = new Mock<ICharacter>();
            var resource = new Mock<IResource>();
            var activationEffect = new Mock<IStanceActivationEffect>();

            return new(character.Object, resource.Object, activationEffect.Object, Stance.Dexterity);
        }
    }
}
