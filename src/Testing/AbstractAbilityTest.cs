namespace PlaygroundTest
{
    using PlaygroundTest.Logik;

    [TestClass]
    public class AbstractAbilityTest
    {
        [TestMethod]
        public void ImplementedAbilityType()
        {
            var ability = new TestAbilityBuffPlayer();


            Assert.IsTrue(ability.TargetType == typeof(TestPlayer));
        }
    }
}
